module Shopfoo.Program.Tests.Helpers

open Shopfoo.Program

type LightStep = { InstructionName: string; Status: StepStatus }

let lightHistory (saga: SagaState) =
    saga.History
    |> List.map (fun step -> { InstructionName = step.Instruction.Name; Status = step.Status })
