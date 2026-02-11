module Shopfoo.Program.Tests.Helpers

open Shopfoo.Program

type LightStep = string * StepStatus

module LightStep =
    let ofProgramStep (step: ProgramStep) = LightStep(step.Instruction.Name, step.Status)

let lightHistory (saga: SagaState) = saga.History |> List.map LightStep.ofProgramStep