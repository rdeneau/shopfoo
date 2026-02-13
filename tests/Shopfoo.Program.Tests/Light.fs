/// This module provides a simplified representation of the types for testing purposes.
module Shopfoo.Program.Tests.Light

open Shopfoo.Product.Tests.OrderContext
open Shopfoo.Program

type LightOrderStatus =
    | LightOrderCreated
    | LightOrderCancelled of previous: LightOrderStatus
    | LightOrderInvoiced
    | LightOrderPaid
    | LightOrderShipped

type LightOrder = {
    Id: OrderId
    Price: decimal
    LightStatus: LightOrderStatus
}

type LightInstruction = string
type LightStep = LightInstruction * StepStatus

let rec lightOrderStatus =
    function
    | OrderCancelled previous -> LightOrderCancelled(lightOrderStatus previous)
    | OrderCreated -> LightOrderCreated
    | OrderInvoiced _ -> LightOrderInvoiced
    | OrderPaid _ -> LightOrderPaid
    | OrderShipped _ -> LightOrderShipped

let lightOrder (order: Order) = {
    Id = order.Id
    Price = order.Price
    LightStatus = lightOrderStatus order.Status
}

let lightStep (step: ProgramStep) : LightStep = step.Instruction.Name, step.Status

let lightHistory (saga: SagaState) = saga.History |> List.map lightStep