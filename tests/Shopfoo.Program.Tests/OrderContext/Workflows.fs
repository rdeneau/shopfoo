module Shopfoo.Product.Tests.OrderContext.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests.OrderContext
open Shopfoo.Program
open Shopfoo.Program.Runner

type Cmder(orderId) =
    member _.ChangeOrderStatus(from, _to) : Cmd.ChangeOrderStatus = {
        OrderId = orderId
        CurrentStatus = from
        NewStatus = _to
    }

    member _.CreateOrder price : Cmd.CreateOrder = { OrderId = orderId; Price = price }
    member _.IssueInvoice amount : Cmd.IssueInvoice = { OrderId = orderId; Amount = amount }
    member _.NotifyOrderChanged newStatus : Cmd.NotifyOrderChanged = { OrderId = orderId; NewStatus = newStatus }
    member _.ProcessPayment amount : Cmd.ProcessPayment = { OrderId = orderId; Amount = amount }
    member _.ShipOrder() : Cmd.ShipOrder = { OrderId = orderId }

[<Interface>]
type IOrderInstructions =
    inherit IProgramInstructions
    abstract member ChangeOrderStatus: (Cmd.ChangeOrderStatus -> Async<Result<unit, Error>>)
    abstract member CreateOrder: (Cmd.CreateOrder -> Async<Result<unit, Error>>)
    abstract member ProcessPayment: (Cmd.ProcessPayment -> Async<Result<PaymentId, Error>>)
    abstract member IssueInvoice: (Cmd.IssueInvoice -> Async<Result<InvoiceId, Error>>)
    abstract member SendNotification: (Cmd.NotifyOrderChanged -> Async<Result<unit, Error>>)
    abstract member ShipOrder: (Cmd.ShipOrder -> Async<Result<ParcelId, Error>>)

module Program =
    type private DefineProgram = DefineProgram<IOrderInstructions>

    let changeOrderStatus cmd = DefineProgram.instruction _.ChangeOrderStatus(cmd)
    let createOrder order = DefineProgram.instruction _.CreateOrder(order)
    let processPayment cmd = DefineProgram.instruction _.ProcessPayment(cmd)
    let issueInvoice cmd = DefineProgram.instruction _.IssueInvoice(cmd)
    let sendNotification cmd = DefineProgram.instruction _.SendNotification(cmd)
    let shipOrder orderId = DefineProgram.instruction _.ShipOrder(orderId)

[<RequireQualifiedAccess>]
type OrderWorkflowStep =
    | CreateOrder
    | ProcessPayment
    | IssueInvoice
    | SendMessage
    | ShipOrder

type OrderWorkflow(?cancelAfterStep: OrderWorkflowStep) =
    interface IProgramWorkflow<IOrderInstructions, Cmd.CreateOrder, unit> with
        override _.Run({ OrderId = orderId; Price = orderPrice } as cmd) =
            let cmder = Cmder orderId

            let cancelAfter step actualStatus =
                program {
                    if cancelAfterStep = Some step then
                        return! Program.changeOrderStatus (cmder.ChangeOrderStatus(from = actualStatus, _to = OrderStatus.Cancelled))
                    else
                        return Ok()
                }

            program {
                // CreateOrder
                do! Program.createOrder cmd
                let currentStatus = OrderStatus.Created
                do! cancelAfter OrderWorkflowStep.CreateOrder currentStatus

                // ProcessPayment
                let! (paymentId: PaymentId) = Program.processPayment { OrderId = orderId; Amount = orderPrice }
                let currentStatus, previousStatus = OrderStatus.PaymentProcessed paymentId, currentStatus
                do! Program.changeOrderStatus (cmder.ChangeOrderStatus(from = previousStatus, _to = currentStatus))
                do! Program.sendNotification (cmder.NotifyOrderChanged currentStatus)
                do! cancelAfter OrderWorkflowStep.ProcessPayment currentStatus

                // IssueInvoice
                let! (invoiceId: InvoiceId) = Program.issueInvoice { OrderId = orderId; Amount = orderPrice }
                let currentStatus, previousStatus = OrderStatus.InvoiceIssued invoiceId, currentStatus
                do! Program.changeOrderStatus (cmder.ChangeOrderStatus(from = previousStatus, _to = currentStatus))
                do! Program.sendNotification (cmder.NotifyOrderChanged currentStatus)
                do! cancelAfter OrderWorkflowStep.IssueInvoice currentStatus

                // ShipOrder
                let! (parcelId: ParcelId) = Program.shipOrder { Cmd.ShipOrder.OrderId = orderId }
                let currentStatus, previousStatus = OrderStatus.Shipped parcelId, currentStatus
                do! Program.changeOrderStatus (cmder.ChangeOrderStatus(from = previousStatus, _to = currentStatus))
                do! Program.sendNotification (cmder.NotifyOrderChanged currentStatus)
                do! cancelAfter OrderWorkflowStep.ShipOrder currentStatus

                return Ok()
            }