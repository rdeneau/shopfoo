module Shopfoo.Program.Runner

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors

[<Sealed>]
type internal SagaTracker<'ins when Instructions<'ins>>() =
    let mutable currentState = { Status = SagaStatus.Running; History = [] }
    let lockObj = obj ()

    member _.CurrentState = currentState
    member _.SetStatus status = lock lockObj (fun () -> currentState <- { currentState with Status = status })
    member _.EnqueueStep step = lock lockObj (fun () -> currentState <- { currentState with History = step :: currentState.History })

type Res<'ret> = Result<'ret, Error>

[<Interface>]
type IProgramWorkflow<'ins, 'arg, 'ret when 'ins :> IProgramInstructions> =
    abstract member Run: 'arg -> Program<'ins, Res<'ret>>

type Work<'arg, 'ret> = 'arg -> Async<'ret>
type WorkMonitor<'arg, 'ret> = delegate of name: string * work: Work<'arg, 'ret> -> Work<'arg, 'ret>

[<Interface>]
type IWorkLogger =
    abstract member Logger: unit -> WorkMonitor<'arg, 'ret>

[<Interface>]
type IWorkMonitors =
    abstract member LoggerFactory: categoryName: string -> IWorkLogger
    abstract member CommandTimer: unit -> WorkMonitor<'arg, Res<'ret>>
    abstract member QueryTimer: unit -> WorkMonitor<'arg, 'ret option>

[<Interface>]
type IWorkCommandBuilder<'arg, 'res> =
    abstract member NoUndo: unit -> Work<'arg, 'res>
    abstract member Revert: undoFun: ('arg -> 'res -> Async<Result<unit, Error>>) -> Work<'arg, 'res>
    abstract member Compensate: undoFun: ('arg -> 'res -> Async<Result<unit, Error>>) -> Work<'arg, 'res>

[<Interface>]
type IInstructionPreparer<'ins when Instructions<'ins>> =
    abstract member Query: work: Work<'arg, 'ret option> * getName: ('arg -> string) -> Work<'arg, 'ret option>
    abstract member Command: work: Work<'arg, Res<'ret>> * getName: ('arg -> string) -> IWorkCommandBuilder<'arg, Res<'ret>>

type IInstructionPreparer<'ins when Instructions<'ins>> with
    member this.Query(work: Work<'arg, 'ret option>, name) = this.Query(work, fun _ -> name)
    member this.Command(work: Work<'arg, Res<'ret>>, name) = this.Command(work, fun _ -> name)

[<Sealed>]
type internal WorkCommandBuilder<'arg, 'res>(build: ('arg -> 'res -> Undo option) -> Work<'arg, 'res>) =
    interface IWorkCommandBuilder<'arg, 'res> with
        member _.NoUndo() = build (fun _ _ -> None)
        member _.Revert undoFun = build (fun arg res -> Some(Undo.Revert(fun () -> undoFun arg res)))
        member _.Compensate undoFun = build (fun arg res -> Some(Undo.Compensate(fun () -> undoFun arg res)))

[<Interface>]
type private IInstructionOptions<'arg, 'ret> =
    abstract member GetInstructionName: ('arg -> string)
    abstract member GetInstructionType: ('arg -> 'ret -> InstructionType)
    abstract member GetTimer: (IWorkMonitors -> WorkMonitor<'arg, 'ret>)
    abstract member ToResult: ('ret -> Result<unit, Error>)

[<Sealed>]
type internal InstructionPreparer<'ins when Instructions<'ins>>(domainName: string, monitors: IWorkMonitors, tracker: SagaTracker<'ins>) =
    let loggerFactory = monitors.LoggerFactory(categoryName = $"Shopfoo.%s{domainName}.Workflow")

    let prepare work (options: IInstructionOptions<'arg, 'ret>) args =
        let instructionName = options.GetInstructionName args
        let loggedWork = loggerFactory.Logger().Invoke(instructionName, work)
        let timedWorks = options.GetTimer(monitors).Invoke(instructionName, loggedWork)

        async {
            let! ret = timedWorks args

            let stepStatus =
                match options.ToResult ret with
                | Ok _ -> RunDone
                | Error err -> (RunFailed err)

            let meta = { Name = instructionName; Type = options.GetInstructionType args ret }
            tracker.EnqueueStep { Instruction = meta; Status = stepStatus }

            return ret
        }

    interface IInstructionPreparer<'ins> with
        member _.Query(work, getName) =
            prepare
                work
                { new IInstructionOptions<_, _> with
                    member _.GetInstructionName = getName
                    member _.GetInstructionType = fun _ _ -> InstructionType.Query
                    member _.GetTimer = _.QueryTimer()
                    member _.ToResult = fun _ -> Ok()
                }

        member _.Command(work, getName) =
            let build getUndo =
                prepare
                    work
                    { new IInstructionOptions<_, _> with
                        member _.GetInstructionName = getName
                        member _.GetInstructionType = fun arg ret -> InstructionType.Command(getUndo arg ret)
                        member _.GetTimer = _.CommandTimer()
                        member _.ToResult = Result.ignore
                    }

            WorkCommandBuilder(build)

[<Interface>]
type internal IInstructionPreparerFactory<'ins when Instructions<'ins>> =
    abstract member Create: SagaTracker<'ins> -> IInstructionPreparer<'ins>

[<Interface>]
type IWorkflowRunner<'ins when Instructions<'ins>> =
    abstract member Run:
        workflow: #IProgramWorkflow<'ins, 'arg, 'ret> ->
        arg: 'arg ->
        prepareInstructions: (IInstructionPreparer<'ins> -> 'ins) ->
            Async<Result<'ret, Error>>

    abstract member RunInSaga:
        workflow: #IProgramWorkflow<'ins, 'arg, 'ret> ->
        arg: 'arg ->
        prepareInstructions: (IInstructionPreparer<'ins> -> 'ins) ->
            Async<Result<'ret, Error> * SagaState>

[<Sealed>]
type internal WorkflowRunner<'ins when Instructions<'ins>>(instructionPreparerFactory: IInstructionPreparerFactory<'ins>) =
    let runWithCanUndo
        (canUndo: bool)
        (workflow: #IProgramWorkflow<'ins, 'arg, 'ret>)
        (arg: 'arg)
        (prepareInstructions: IInstructionPreparer<'ins> -> 'ins)
        : Async<Result<'ret, Error> * SagaState> =
        async {
            let tracker = SagaTracker<'ins>()

            try
                let instructionPreparer = instructionPreparerFactory.Create tracker
                let instructions = prepareInstructions instructionPreparer

                let! result = workflow.Run arg instructions
                let sagaStateAfterRun = tracker.CurrentState

                let! sagaFinalState =
                    match result with
                    | Error _ when canUndo -> Saga.performUndo sagaStateAfterRun
                    | _ -> async { return sagaStateAfterRun }

                return result, sagaFinalState

            with FirstException exn ->
                let! sagaStateAfterUndo =
                    if canUndo then
                        Saga.performUndo tracker.CurrentState
                    else
                        async { return tracker.CurrentState }

                return bug exn, sagaStateAfterUndo
        }

    interface IWorkflowRunner<'ins> with
        member _.Run workflow arg prepareInstructions =
            async {
                let! result, _ = runWithCanUndo false workflow arg prepareInstructions
                return result
            }

        member _.RunInSaga workflow arg prepareInstructions = // ↩
            runWithCanUndo true workflow arg prepareInstructions