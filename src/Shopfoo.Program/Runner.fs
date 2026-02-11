module Shopfoo.Program.Runner

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors

[<Interface>]
type IProgramWorkflow<'ins, 'arg, 'ret when 'ins :> IProgramInstructions> =
    abstract member Run: 'arg -> Program<'ins, Result<'ret, Error>>

type Work<'arg, 'ret> = 'arg -> Async<'ret>
type WorkMonitor<'arg, 'ret> = delegate of name: string * work: Work<'arg, 'ret> -> Work<'arg, 'ret>

[<Interface>]
type IWorkLogger =
    abstract member Logger: unit -> WorkMonitor<'arg, 'ret>

[<Interface>]
type IWorkMonitors =
    abstract member LoggerFactory: categoryName: string -> IWorkLogger
    abstract member CommandTimer: unit -> WorkMonitor<'arg, Result<'ret, Error>>
    abstract member QueryTimer: unit -> WorkMonitor<'arg, 'ret option>

[<Interface>]
type IInstructionOptions<'arg, 'ret> =
    abstract member GetInstructionType: 'arg -> 'ret -> InstructionType
    abstract member GetTimer: IWorkMonitors -> WorkMonitor<'arg, 'ret>
    abstract member ToResult: 'ret -> Result<unit, Error>

[<Sealed>]
type QueryOptions<'arg, 'ret>() =
    interface IInstructionOptions<'arg, 'ret option> with
        member _.GetInstructionType _ _ = InstructionType.Query
        member _.GetTimer monitors = monitors.QueryTimer()
        member _.ToResult _ = Ok()

[<Sealed>]
type CommandOptions<'arg, 'ret>(getType) =
    interface IInstructionOptions<'arg, Result<'ret, Error>> with
        member _.GetInstructionType arg result = getType arg result
        member _.GetTimer monitors = monitors.CommandTimer()
        member _.ToResult result = result |> Result.ignore

[<Sealed>]
type CommandOptionsBuilder() =
    static member val Instance = CommandOptionsBuilder()
    member _.NoUndo() : IInstructionOptions<_, _> = CommandOptions(fun _ _ -> InstructionType.Command(undo = None))

    member _.Revert undoFun =
        CommandOptions(fun arg result -> InstructionType.Command(undo = Some(UndoType.Revert, UndoFunc(fun () -> undoFun arg result))))

    member _.Compensate undoFun =
        CommandOptions(fun arg result -> InstructionType.Command(undo = Some(UndoType.Compensate, UndoFunc(fun () -> undoFun arg result))))

[<Sealed>]
type InstructionOptionsBuilder() =
    static member val Instance = InstructionOptionsBuilder()
    member _.Query() : IInstructionOptions<_, _> = QueryOptions<'arg, 'ret>()
    member _.Command = CommandOptionsBuilder.Instance

[<Interface>]
type IWorkflowPreparer<'ins when Instructions<'ins>> =
    abstract member PrepareInstruction:
        name: string -> work: Work<'arg, 'ret> -> buildOptions: (InstructionOptionsBuilder -> IInstructionOptions<'arg, 'ret>) -> Work<'arg, 'ret>

[<Sealed>]
type internal SagaTracker<'ins when Instructions<'ins>>() =
    let mutable currentState = { Status = SagaStatus.Running; History = [] }
    let lockObj = obj ()

    member _.CurrentState = currentState
    member _.SetStatus status = lock lockObj (fun () -> currentState <- { currentState with Status = status })
    member _.EnqueueStep step = lock lockObj (fun () -> currentState <- { currentState with History = step :: currentState.History })

[<Sealed>]
type internal WorkflowPreparer<'ins when Instructions<'ins>>(domainName: string, monitors: IWorkMonitors, tracker: SagaTracker<'ins>) =
    let loggerFactory = monitors.LoggerFactory(categoryName = $"Shopfoo.%s{domainName}.Workflow")

    interface IWorkflowPreparer<'ins> with
        member _.PrepareInstruction name work buildOptions =
            let options = buildOptions InstructionOptionsBuilder.Instance
            let loggedWork = loggerFactory.Logger().Invoke(name, work)
            let timedWorks = options.GetTimer(monitors).Invoke(name, loggedWork)

            fun args ->
                async {
                    let! ret = timedWorks args

                    let stepStatus =
                        match options.ToResult ret with
                        | Ok _ -> StepStatus.RunDone
                        | Error err -> (StepStatus.RunFailed err)

                    let meta = { Name = name; Type = options.GetInstructionType args ret }
                    tracker.EnqueueStep { Instruction = meta; Status = stepStatus }

                    return ret
                }

[<Interface>]
type internal IWorkflowPreparerFactory<'ins when Instructions<'ins>> =
    abstract member Create: SagaTracker<'ins> -> IWorkflowPreparer<'ins>

[<Interface>]
type IWorkflowRunner<'ins when Instructions<'ins>> =
    abstract member Run:
        workflow: #IProgramWorkflow<'ins, 'arg, 'ret> ->
        arg: 'arg ->
        prepareInstructions: (IWorkflowPreparer<'ins> -> 'ins) ->
            Async<Result<'ret, Error>>

    abstract member RunInSaga:
        workflow: #IProgramWorkflow<'ins, 'arg, 'ret> ->
        arg: 'arg ->
        prepareInstructions: (IWorkflowPreparer<'ins> -> 'ins) ->
            Async<Result<'ret, Error> * SagaState>

[<Sealed>]
type internal WorkflowRunner<'ins when Instructions<'ins>>(workflowPreparerFactory: IWorkflowPreparerFactory<'ins>) =
    let runWithCanUndo
        (canUndo: bool)
        (workflow: #IProgramWorkflow<'ins, 'arg, 'ret>)
        (arg: 'arg)
        (prepareInstructions: IWorkflowPreparer<'ins> -> 'ins)
        : Async<Result<'ret, Error> * SagaState> =
        async {
            let tracker = SagaTracker<'ins>()

            try
                let workflowPreparer = workflowPreparerFactory.Create tracker
                let instructions = prepareInstructions workflowPreparer

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