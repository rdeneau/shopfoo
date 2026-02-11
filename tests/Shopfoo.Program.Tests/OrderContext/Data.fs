module Shopfoo.Product.Tests.OrderContext.Data

open System.Runtime.CompilerServices
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests.OrderContext

/// Helpers to ease creating errors from commands
[<AutoOpen>]
module private CmdErrorExtensions =
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