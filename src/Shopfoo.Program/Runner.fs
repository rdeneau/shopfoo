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
type IWorkflowPreparer<'ins when Instructions<'ins>> =
    abstract member PrepareQuery:  // ↩
        name: string * work: Work<'arg, 'ret option> -> Work<'arg, 'ret option>

    abstract member PrepareCommand:
        name: string * work: Work<'arg, Result<'ret, Error>> * ?undo: (UndoType * UndoFunc) -> Work<'arg, Result<'ret, Error>>

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

    let prepareInstruction
        (timerFactory: unit -> WorkMonitor<'arg, 'ret>)
        (meta: InstructionMeta)
        (work: Work<'arg, 'ret>)
        (toResult: 'ret -> Result<unit, Error>)
        : Work<'arg, 'ret> =
        let loggedWork = loggerFactory.Logger().Invoke(meta.Name, work)
        let timedWorks = timerFactory().Invoke(meta.Name, loggedWork)

        let trackStep status = tracker.EnqueueStep { Instruction = meta; Status = status }

        fun args ->
            async {
                let! ret = timedWorks args

                match toResult ret with
                | Ok() -> trackStep StepStatus.RunDone
                | Error err -> trackStep (StepStatus.RunFailed err)

                return ret
            }

    interface IWorkflowPreparer<'ins> with
        member _.PrepareQuery(name, work) : Work<'arg, 'ret option> =
            prepareInstruction monitors.QueryTimer { Name = name; Type = InstructionType.Query } work (fun _ -> Ok())

        member _.PrepareCommand(name, work, ?undo) : Work<'arg, Result<'ret, Error>> =
            prepareInstruction monitors.CommandTimer { Name = name; Type = InstructionType.Command undo } work Result.ignore

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