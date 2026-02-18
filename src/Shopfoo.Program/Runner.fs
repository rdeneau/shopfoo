module Shopfoo.Program.Runner

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors

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
        undoPredicate: CanUndo ->
            Async<Result<'ret, Error> * SagaState>

[<AutoOpen>]
module Implementation =
    [<Interface>]
    type private IInstructionOptions<'arg, 'ret> =
        abstract member GetInstructionName: ('arg -> string)
        abstract member GetInstructionType: ('arg -> 'ret -> InstructionType)
        abstract member GetTimer: (IWorkMonitors -> WorkMonitor<'arg, 'ret>)
        abstract member ToResult: ('ret -> Result<unit, Error>)

    [<Sealed>]
    type internal SagaTracker<'ins when Instructions<'ins>>() =
        let mutable history = []
        let lockObj = obj ()

        member _.History = history
        member _.EnqueueStep step = lock lockObj (fun () -> history <- step :: history)

    [<Sealed>]
    type private WorkCommandBuilder<'arg, 'res>(build: ('arg -> 'res -> Undo option) -> Work<'arg, 'res>) =
        interface IWorkCommandBuilder<'arg, 'res> with
            member _.NoUndo() = build (fun _ _ -> None)
            member _.Revert undoFun = build (fun arg res -> Some(Undo.Revert(fun () -> undoFun arg res)))
            member _.Compensate undoFun = build (fun arg res -> Some(Undo.Compensate(fun () -> undoFun arg res)))

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

    [<Sealed>]
    type internal WorkflowRunner<'ins when Instructions<'ins>>(instructionPreparerFactory: IInstructionPreparerFactory<'ins>) =
        let runWorkflowWith
            (undoPredicate: CanUndo)
            (workflow: #IProgramWorkflow<'ins, 'arg, 'ret>)
            (arg: 'arg)
            (prepareInstructions: IInstructionPreparer<'ins> -> 'ins)
            : Async<Result<'ret, Error> * SagaState> =
            async {
                let tracker = SagaTracker<'ins>()
                let instructionPreparer = instructionPreparerFactory.Create tracker
                let instructions = prepareInstructions instructionPreparer

                let finalizeSaga result =
                    async {
                        let! sagaFinalState = Saga.finalize undoPredicate result tracker.History
                        return result, sagaFinalState
                    }

                try
                    let! result = workflow.Run arg instructions
                    return! finalizeSaga result

                with FirstException exn ->
                    return! finalizeSaga (Error(Bug exn))
            }

        interface IWorkflowRunner<'ins> with
            member _.Run workflow arg prepareInstructions =
                async {
                    let! result, _ = runWorkflowWith CanUndo.never workflow arg prepareInstructions
                    return result
                }

            member _.RunInSaga workflow arg prepareInstructions undoPredicate = // ↩
                runWorkflowWith undoPredicate workflow arg prepareInstructions