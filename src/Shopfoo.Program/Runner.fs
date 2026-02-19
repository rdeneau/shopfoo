module Shopfoo.Program.Runner

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
type IWorkCommandBuilder<'arg, 'ret> =
    abstract member NotUndoable: unit -> Work<'arg, Res<'ret>>
    abstract member Reversible: undoFun: ('arg -> 'ret -> Async<Res<unit>>) -> Work<'arg, Res<'ret>>
    abstract member Compensatable: undoFun: ('arg -> 'ret -> Async<Res<unit>>) -> Work<'arg, Res<'ret>>

[<Interface>]
type IInstructionPreparer<'ins when Instructions<'ins>> =
    abstract member Query: work: Work<'arg, 'ret option> * getName: ('arg -> string) -> Work<'arg, 'ret option>
    abstract member Command: work: Work<'arg, Res<'ret>> * getName: ('arg -> string) -> IWorkCommandBuilder<'arg, 'ret>

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
    [<Sealed>]
    type internal SagaTracker<'ins when Instructions<'ins>>() =
        let mutable history = []
        let lockObj = obj ()

        let enqueueStep name type' status =
            lock lockObj
            <| fun () ->
                let step = { Instruction = { Name = name; Type = type' }; Status = status }
                history <- step :: history

        member _.History = history
        member _.EnqueueQuery name = enqueueStep name InstructionType.Query RunDone
        member _.EnqueueCommand name status undo = enqueueStep name (InstructionType.Command undo) status

    [<Sealed>]
    type private WorkCommandBuilder<'arg, 'ret>(build: ('arg -> 'ret -> Undo) -> 'arg -> Async<Res<'ret>>) =
        interface IWorkCommandBuilder<'arg, 'ret> with
            member _.NotUndoable() = build (fun _ _ -> Undo.None)
            member _.Reversible undoFun = build (fun arg ret -> Undo.Revert(UndoFunc(fun () -> undoFun arg ret)))
            member _.Compensatable undoFun = build (fun arg ret -> Undo.Compensate(UndoFunc(fun () -> undoFun arg ret)))

    [<Sealed>]
    type internal InstructionPreparer<'ins when Instructions<'ins>>(domainName: string, monitors: IWorkMonitors, tracker: SagaTracker<'ins>) =
        let loggerFactory = monitors.LoggerFactory(categoryName = $"Shopfoo.%s{domainName}.Workflow")

        let runAndMonitor work getName getTimer args =
            async {
                let name = getName args
                let timer: WorkMonitor<_, _> = getTimer monitors
                let loggedWork = loggerFactory.Logger().Invoke(name, work)
                let timedWorks = timer.Invoke(name, loggedWork)
                let! ret = timedWorks args
                return name, ret
            }

        interface IInstructionPreparer<'ins> with
            member _.Query(work, getName) =
                fun args ->
                    async {
                        let! name, ret = runAndMonitor work getName _.QueryTimer() args
                        tracker.EnqueueQuery name
                        return ret
                    }

            member _.Command(work, getName) =
                let build getUndo args =
                    async {
                        let! name, ret = runAndMonitor work getName _.CommandTimer() args

                        match ret with
                        | Ok res -> tracker.EnqueueCommand name RunDone (getUndo args res)
                        | Error err -> tracker.EnqueueCommand name (RunFailed err) Undo.None

                        return ret
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