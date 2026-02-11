namespace Shopfoo.Program

open Shopfoo.Domain.Types.Errors

[<RequireQualifiedAccess>]
type UndoType =
    /// <summary>
    /// Strict undo: should revert to the initial state—e.g., Database <c>DELETE</c> to undo an <c>INSERT</c>.
    /// </summary>
    | Revert

    /// <summary>
    /// Loose undo:
    /// <br /> - attempt to revert the effect but keep a trace—e.g., `RefundPayment` to compensate for a `ChargePayment` Command.
    /// <br /> - revert with no guarantee—e.g., `CancelEmail` Message.
    /// <br /> - mitigate the effect—e.g., send a "Sorry" email.
    /// </summary>
    | Compensate

type UndoFunc([<InlineIfLambda>] func: unit -> Async<Result<unit, Error>>) =
    static let hashCode = hash "UndoFunc"
    member _.Invoke() = func ()
    override _.Equals(other) = (hash other = hashCode)
    override _.GetHashCode() = hashCode

[<RequireQualifiedAccess>]
type InstructionType =
    | Query
    | Command of undo: (UndoType * UndoFunc) option

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
    | Failed of originalError: Error * undoErrors: Error list

/// <summary>
/// Indicates the current status of the Saga workflow, including:
/// <br /> - the history of executed steps stored in LIFO order for undo purposes.
/// <br /> - an optional CancellationTokenSource to signal cancellation to running steps if needed during the undo.
/// </summary>
type SagaState = { Status: SagaStatus; History: ProgramStep list }

[<RequireQualifiedAccess>]
module Saga =
    let start<'ret> () : SagaState = { Status = SagaStatus.Running; History = [] }

    /// Performs undo for all successfully completed commands in reverse order (LIFO)
    /// Returns the updated saga state with undo results
    let performUndo (state: SagaState) : Async<SagaState> =
        async {
            match state.History with
            | { Status = RunFailed originalError } :: stepsToUndo ->
                let undoErrors = ResizeArray<Error>()
                let updatedSteps = ResizeArray<ProgramStep>()

                // Process history in order (already prepended, so oldest last)
                for step in stepsToUndo do
                    let! updatedStep =
                        async {
                            match step.Status, step.Instruction.Type with
                            | RunDone, InstructionType.Command(Some(_, undoFunc)) ->
                                // Execute undo function
                                let! undoResult = undoFunc.Invoke()

                                return
                                    match undoResult with
                                    | Ok() -> { step with Status = UndoDone }
                                    | Error err ->
                                        undoErrors.Add(err)
                                        { step with Status = UndoFailed err }

                            | RunDone, (InstructionType.Query | InstructionType.Command(undo = None))
                            | RunFailed _, _
                            | UndoDone, _
                            | UndoFailed _, _ -> return step
                        }

                    updatedSteps.Add updatedStep

                return { state with Status = SagaStatus.Failed(originalError, undoErrors |> List.ofSeq); History = updatedSteps |> List.ofSeq }

            | _ ->
                let error = Bug(exn "performUndo called but no failed step found in the head of the history")
                return { state with Status = SagaStatus.Failed(error, []) }
        }