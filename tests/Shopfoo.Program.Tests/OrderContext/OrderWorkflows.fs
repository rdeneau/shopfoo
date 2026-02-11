module Shopfoo.Product.Tests.OrderContext.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests.OrderContext
open Shopfoo.Program
open Shopfoo.Program.Runner

type Cmder(orderId) =
    member _.CreateOrder price : Cmd.CreateOrder = { OrderId = orderId; Price = price }
    member _.IssueInvoice amount : Cmd.IssueInvoice = { OrderId = orderId; Amount = amount }
    member _.NotifyOrderChanged newStatus : Cmd.NotifyOrderChanged = { OrderId = orderId; NewStatus = newStatus }
    member _.ProcessPayment amount : Cmd.ProcessPayment = { OrderId = orderId; Amount = amount }
    member _.ShipOrder() : Cmd.ShipOrder = { OrderId = orderId }
    member _.TransitionOrder transition : Cmd.TransitionOrder = { OrderId = orderId; Transition = transition }

[<Interface>]
type IOrderInstructions =
    inherit IProgramInstructions
    abstract member CreateOrder: (Cmd.CreateOrder -> Async<Result<unit, Error>>)
    abstract member ProcessPayment: (Cmd.ProcessPayment -> Async<Result<PaymentId, Error>>)
    abstract member IssueInvoice: (Cmd.IssueInvoice -> Async<Result<InvoiceId, Error>>)
    abstract member SendNotification: (Cmd.NotifyOrderChanged -> Async<Result<unit, Error>>)
    abstract member ShipOrder: (Cmd.ShipOrder -> Async<Result<ParcelId, Error>>)
    abstract member TransitionOrder: (Cmd.TransitionOrder -> Async<Result<unit, Error>>)

module Program =
    type private DefineProgram = DefineProgram<IOrderInstructions>

    let createOrder order = DefineProgram.instruction _.CreateOrder(order)
    let processPayment cmd = DefineProgram.instruction _.ProcessPayment(cmd)
    let issueInvoice cmd = DefineProgram.instruction _.IssueInvoice(cmd)
    let sendNotification cmd = DefineProgram.instruction _.SendNotification(cmd)
    let shipOrder orderId = DefineProgram.instruction _.ShipOrder(orderId)
    let transitionOrder cmd = DefineProgram.instruction _.TransitionOrder(cmd)

[<RequireQualifiedAccess>]
type OrderAction =
    | CreateOrder
    | ProcessPayment
    | IssueInvoice
    | SendNotification
    | ShipOrder

type OrderWorkflow(?cancelAfterStep: OrderAction) =
    interface IProgramWorkflow<IOrderInstructions, Cmd.CreateOrder, unit> with
        override _.Run({ OrderId = orderId; Price = orderPrice } as cmd) =
            let cmder = Cmder orderId

            let cancelAfter step actualStatus =
                program {
                    if cancelAfterStep = Some step then
                        return! Program.transitionOrder (cmder.TransitionOrder { From = actualStatus; To = OrderCancelled })
                    else
                        return Ok()
                }

            program {
                // CreateOrder
                do! Program.createOrder cmd
                let currentStatus = OrderCreated
                do! cancelAfter OrderAction.CreateOrder currentStatus

                // ProcessPayment
                let! (paymentId: PaymentId) = Program.processPayment { OrderId = orderId; Amount = orderPrice }
                let currentStatus, previousStatus = OrderPaid paymentId, currentStatus
                do! Program.transitionOrder (cmder.TransitionOrder { From = previousStatus; To = currentStatus })
                do! Program.sendNotification (cmder.NotifyOrderChanged currentStatus)
                do! cancelAfter OrderAction.ProcessPayment currentStatus

                // IssueInvoice
                let! (invoiceId: InvoiceId) = Program.issueInvoice { OrderId = orderId; Amount = orderPrice }
                let currentStatus, previousStatus = OrderInvoiced invoiceId, currentStatus
                do! Program.transitionOrder (cmder.TransitionOrder { From = previousStatus; To = currentStatus })
                do! Program.sendNotification (cmder.NotifyOrderChanged currentStatus)
                do! cancelAfter OrderAction.IssueInvoice currentStatus

                // ShipOrder
                let! (parcelId: ParcelId) = Program.shipOrder { Cmd.ShipOrder.OrderId = orderId }
                let currentStatus, previousStatus = OrderShipped parcelId, currentStatus
                do! Program.transitionOrder (cmder.TransitionOrder { From = previousStatus; To = currentStatus })
                do! Program.sendNotification (cmder.NotifyOrderChanged currentStatus)
                do! cancelAfter OrderAction.ShipOrder currentStatus

                return Ok()
            }