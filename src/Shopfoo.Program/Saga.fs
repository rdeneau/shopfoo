namespace Shopfoo.Program

open Shopfoo.Domain.Types.Errors

/// Wraps an undo function. Can be placed in structural types (records, DU) without causing equality issues.
type UndoFunc([<InlineIfLambda>] func: unit -> Async<Result<unit, Error>>) =
    static let hashCode = hash (nameof UndoFunc)
    member _.Invoke() = func ()
    override _.Equals(other) = (hash other = hashCode)
    override _.GetHashCode() = hashCode

[<RequireQualifiedAccess>]
type Undo =
    | None

    /// <summary>
    /// Strict undo: should revert to the initial state—e.g., Database <c>DELETE</c> to undo an <c>INSERT</c>.
    /// </summary>
    | Revert of UndoFunc

    /// <summary>
    /// Loose undo:
    /// <br /> - attempt to revert the effect but keep a trace—e.g., `RefundPayment` to compensate for a `ChargePayment` Command.
    /// <br /> - revert with no guarantee—e.g., `CancelEmail` Message.
    /// <br /> - mitigate the effect—e.g., send a "Sorry" email.
    /// </summary>
    | Compensate of UndoFunc

[<RequireQualifiedAccess>]
type InstructionType =
    | Query
    | Command of undo: Undo

type InstructionMeta = { Name: string; Type: InstructionType }

type StepStatus =
    | RunDone
    | RunFailed of runError: Error
    | UndoDone
    | UndoFailed of undoError: Error

type ProgramStep = { Instruction: InstructionMeta; Status: StepStatus }

[<RequireQualifiedAccess>]
type SagaStatus =
    | Running
    | Done
    | Cancelled
    | Failed of originalError: Error * undoErrors: Error list

/// <summary>
/// Indicates the current status of the Saga workflow, including:
/// <br /> - the history of executed steps stored in LIFO order for undo purposes.
/// <br /> - an optional CancellationTokenSource to signal cancellation to running steps if needed during the undo.
/// </summary>
type SagaState = { Status: SagaStatus; History: ProgramStep list }

type UndoCriteria = { WorkflowError: Error; History: ProgramStep list }
type CanUndo = UndoCriteria -> bool

[<RequireQualifiedAccess>]
module CanUndo =
    let never: CanUndo = fun _ -> false
    let always: CanUndo = fun _ -> true

[<RequireQualifiedAccess>]
module Saga =
    type private UndoParameters = { WorkflowError: Error; StepsToUndo: ProgramStep list }

    [<RequireQualifiedAccess>]
    type private SagaIntermediateStatus =
        | PendingUndo of UndoParameters
        | Finalized of finalStatus: SagaStatus

    /// Performs undo for all successfully completed commands in reverse order (LIFO)
    /// Returns the updated saga state with undo results
    let private performUndo (undoParameters: UndoParameters) : Async<SagaState> =
        async {
            let undoErrors = ResizeArray<Error>()
            let updatedSteps = ResizeArray<ProgramStep>()

            // Process history in order (already prepended, so oldest last)
            for step in undoParameters.StepsToUndo do
                let! updatedStep =
                    async {
                        match step.Status, step.Instruction.Type with
                        | RunDone, InstructionType.Command(Undo.Revert undoFunc | Undo.Compensate undoFunc) ->
                            match! undoFunc.Invoke() with
                            | Ok() -> return { step with Status = UndoDone }
                            | Error err ->
                                undoErrors.Add(err)
                                return { step with Status = UndoFailed err }

                        | RunDone, (InstructionType.Query | InstructionType.Command Undo.None)
                        | RunFailed _, _
                        | UndoDone, _
                        | UndoFailed _, _ -> return step
                    }

                updatedSteps.Add updatedStep

            return { Status = SagaStatus.Failed(undoParameters.WorkflowError, undoErrors |> List.ofSeq); History = updatedSteps |> List.ofSeq }
        }

    /// <summary>
    /// Determine the Saga finalized status, including an optional undo phase performed if the workflow has not been cancelled
    /// and if it has failed with an error that meets the undo criteria defined by the provided predicate.
    /// </summary>
    /// <remarks>
    /// Depending on whether the error occurs at the last step—the last instruction—or after it—at the end of the program,
    /// the steps that can potentially be undone vary, respectively the previous instructions or all instructions.
    /// </remarks>
    let finalize (canUndo: CanUndo) (result: Res<'t>) (history: ProgramStep list) : Async<SagaState> =
        async {
            let intermediateStatus =
                match result, history with
                | Ok _, _ -> SagaIntermediateStatus.Finalized SagaStatus.Done
                | Error(WorkflowError(WorkflowCancelled _)), _ -> SagaIntermediateStatus.Finalized SagaStatus.Cancelled
                | Error err, _ when not (canUndo { WorkflowError = err; History = history }) ->
                    SagaIntermediateStatus.Finalized(SagaStatus.Failed(err, undoErrors = []))
                | Error err, { Status = RunFailed lastStepError } :: previousSteps when err = lastStepError ->
                    SagaIntermediateStatus.PendingUndo { WorkflowError = err; StepsToUndo = previousSteps }
                | Error err, _ -> SagaIntermediateStatus.PendingUndo { WorkflowError = err; StepsToUndo = history }

            match intermediateStatus with
            | SagaIntermediateStatus.PendingUndo undoParameters -> return! performUndo undoParameters
            | SagaIntermediateStatus.Finalized finalStatus -> return { History = history; Status = finalStatus }
        }