module Shopfoo.Product.Tests.OrderContext

open System
open System.Runtime.CompilerServices
open Shopfoo.Domain.Types.Errors
open Shopfoo.Program
open Shopfoo.Program.Runner

[<AutoOpen>]
module Model =
    [<AutoOpen>]
    module Id =
        type Id<'kind> = { Kind: 'kind; Value: Guid }

        module Id =
            let New kind = { Kind = kind; Value = Guid.NewGuid() }

        module InvoiceId =
            type Invoice = private | Invoice
            let New () = Id.New Invoice

        module OrderId =
            type Order = private | Order
            let New () = Id.New Order

        module ParcelId =
            type Parcel = private | Parcel
            let New () = Id.New Parcel

        module PaymentId =
            type Payment = private | Payment
            let New () = Id.New Payment

        type InvoiceId = Id<InvoiceId.Invoice>
        type OrderId = Id<OrderId.Order>
        type ParcelId = Id<ParcelId.Parcel>
        type PaymentId = Id<PaymentId.Payment>

    type Invoice = {
        Id: InvoiceId
        OrderId: OrderId
        Amount: decimal
        UtcDate: DateTime
    } with
        static member Create(orderId, amount) = {
            Id = InvoiceId.New()
            OrderId = orderId
            Amount = amount
            UtcDate = DateTime.UtcNow
        }

    [<RequireQualifiedAccess>]
    type OrderStatus =
        | Created
        | Cancelled
        | PaymentProcessed of PaymentId
        | InvoiceIssued of InvoiceId
        | Shipped of ParcelId

    type Order = {
        Id: OrderId
        Price: decimal
        Status: OrderStatus
    } with
        static member Create(price, ?id) = {
            Id = defaultArg id (OrderId.New())
            Price = price
            Status = OrderStatus.Created
        }

    type Payment = {
        Id: PaymentId
        OrderId: OrderId
        Amount: decimal
        UtcDate: DateTime
    } with
        static member Create(orderId, amount) = {
            Id = PaymentId.New()
            OrderId = orderId
            Amount = amount
            UtcDate = DateTime.UtcNow
        }

module Cmd =
    type ChangeOrderStatus = {
        OrderId: OrderId
        CurrentStatus: OrderStatus
        NewStatus: OrderStatus
    } with
        member this.Revert() = { this with CurrentStatus = this.NewStatus; NewStatus = this.CurrentStatus }

    type CreateOrder = { OrderId: OrderId; Price: decimal }
    type IssueInvoice = { OrderId: OrderId; Amount: decimal }
    type CompensateInvoice = { InvoiceId: InvoiceId }
    type NotifyOrderChanged = { OrderId: OrderId; NewStatus: OrderStatus }
    type ProcessPayment = { OrderId: OrderId; Amount: decimal }
    type RefundPayment = { PaymentId: PaymentId }
    type ShipOrder = { OrderId: OrderId }

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

[<AutoOpen>]
module Workflows =
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

/// Helpers to ease creating errors from commands
[<AutoOpen>]
module CmdErrorExtensions =
    type Id<'kind> with
        member private this.Format() = this.Value.ToString "B"

        member this.ToError() = {|
            DuplicateKey = fun () -> Result.Error(Error.DataError(DuplicateKey(Id = this.Format(), Type = $"%A{this.Kind}")))
            NotFound = fun () -> Result.Error(Error.DataError(DataNotFound(Id = this.Format(), Type = $"%A{this.Kind}")))
        |}

    type ChangeOrderStatusError(cmd: Cmd.ChangeOrderStatus) =
        member _.NotAllowedFrom(actualStatus: OrderStatus) =
            let reason =
                $"Cannot change order %A{cmd.OrderId} to %A{cmd.NewStatus}: "
                + $"unexpected current status (expected: %A{cmd.CurrentStatus}, actual: %A{actualStatus})"

            Result.Error(Error.OperationNotAllowed { Operation = "ChangeOrderStatus"; Reason = reason })

        member _.OrderNotFound() = cmd.OrderId.ToError().NotFound()

    type CompensateInvoiceError(cmd: Cmd.CompensateInvoice) =
        member _.InvoiceNotFound() = cmd.InvoiceId.ToError().NotFound()

    type CreateOrderError(cmd: Cmd.CreateOrder) =
        member _.OrderDuplicateKey() = cmd.OrderId.ToError().DuplicateKey()

    type RefundPaymentError(cmd: Cmd.RefundPayment) =
        member _.PaymentNotFound() = cmd.PaymentId.ToError().NotFound()

    type CmdExtensions =
        [<Extension>]
        static member Error(cmd: Cmd.ChangeOrderStatus) = ChangeOrderStatusError cmd

        [<Extension>]
        static member Error(cmd: Cmd.CompensateInvoice) = CompensateInvoiceError cmd

        [<Extension>]
        static member Error(cmd: Cmd.CreateOrder) = CreateOrderError cmd

        [<Extension>]
        static member Error(cmd: Cmd.RefundPayment) = RefundPaymentError cmd

module Data =
    type Repository<'k, 'v when 'k: comparison>(getKey: 'v -> 'k) =
        let mutable repo: Map<'k, 'v> = Map.empty

        member _.AddOrUpdate(value: 'v) =
            async {
                repo <- repo.Add(getKey value, value)
                return Ok()
            }

        member _.FindBy predicate = async { return repo |> Map.toSeq |> Seq.map snd |> Seq.tryFind predicate }
        member _.Get key = async { return repo |> Map.tryFind key }

        member _.Remove key =
            async {
                repo <- repo.Remove key
                return Ok()
            }

    type InvoiceRepository() =
        let invoices = Repository<InvoiceId, Invoice>(_.Id)

        member _.IssueInvoice(?simulatedError) =
            fun (cmd: Cmd.IssueInvoice) ->
                async {
                    match simulatedError with
                    | Some err -> return Error err
                    | None ->
                        let invoice = Invoice.Create(cmd.OrderId, cmd.Amount)
                        let! result = invoices.AddOrUpdate invoice
                        return result |> Result.map (fun () -> invoice.Id)
                }

        member _.CompensateInvoice(cmd: Cmd.CompensateInvoice) =
            async {
                match! invoices.Get cmd.InvoiceId with
                | Some invoice -> return! invoices.AddOrUpdate(Invoice.Create(invoice.OrderId, -invoice.Amount))
                | None -> return cmd.Error().InvoiceNotFound()
            }

    type NotificationClient() =
        member _.SendNotification(?simulatedError) =
            fun (cmd: Cmd.NotifyOrderChanged) ->
                async {
                    match simulatedError with
                    | Some err -> return Error err
                    | None ->
                        printfn $"Sending notification for order %A{cmd.OrderId} changed to %A{cmd.NewStatus}"
                        return Ok()
                }

    type OrderRepository() =
        let orders = Repository<OrderId, Order>(_.Id)

        let getOrderById orderId = orders.Get orderId

        member _.GetOrderById = getOrderById

        member _.CreateOrder(?simulatedError) =
            fun (cmd: Cmd.CreateOrder) ->
                async {
                    match simulatedError with
                    | Some err -> return Error err
                    | None ->
                        match! getOrderById cmd.OrderId with
                        | None -> return! orders.AddOrUpdate(Order.Create(cmd.Price, cmd.OrderId))
                        | Some _ -> return cmd.Error().OrderDuplicateKey()
                }

        member _.DeleteOrder(orderId) = orders.Remove orderId

        member _.ChangeOrderStatus(cmd: Cmd.ChangeOrderStatus) =
            async {
                match! getOrderById cmd.OrderId with
                | Some order when order.Status = cmd.CurrentStatus -> return! orders.AddOrUpdate { order with Status = cmd.NewStatus }
                | Some order -> return cmd.Error().NotAllowedFrom(order.Status)
                | None -> return cmd.Error().OrderNotFound()
            }

    type PaymentRepository() =
        let payments = Repository<PaymentId, Payment>(_.Id)

        member _.ProcessPayment(?simulatedError) =
            fun (cmd: Cmd.ProcessPayment) ->
                async {
                    match simulatedError with
                    | Some err -> return Error err
                    | None ->
                        let payment = Payment.Create(cmd.OrderId, cmd.Amount)
                        let! result = payments.AddOrUpdate payment
                        return result |> Result.map (fun () -> payment.Id)
                }

        member _.RefundPayment(cmd: Cmd.RefundPayment) =
            async {
                match! payments.Get cmd.PaymentId with
                | Some payment -> return! payments.AddOrUpdate(Payment.Create(payment.OrderId, -payment.Amount))
                | None -> return cmd.Error().PaymentNotFound()
            }

    type WarehouseClient() =
        member _.ShipOrder(?simulatedError) =
            fun (cmd: Cmd.ShipOrder) ->
                async {
                    match simulatedError with
                    | Some err -> return Error err
                    | None ->
                        let parcelId = ParcelId.New()
                        printfn $"Shipping order %A{cmd.OrderId} with parcel id %A{parcelId}"
                        return Ok parcelId
                }