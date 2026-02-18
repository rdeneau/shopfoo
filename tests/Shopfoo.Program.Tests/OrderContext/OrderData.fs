module Shopfoo.Product.Tests.OrderContext.Data

open System.Runtime.CompilerServices
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests.OrderContext
open Shopfoo.Product.Tests.OrderContext.Workflows

type SimulatedError = { Error: Error; Step: OrderStep }

[<Interface>]
type ISimulatedErrorProvider =
    abstract member Find: predicate: (OrderStep -> bool) -> Error option

type ISimulatedErrorProvider with
    member this.Find(step: OrderStep) = this.Find((=) step)

type SimulatedErrorProvider() =
    let mutable value: SimulatedError option = None

    member _.Define error = value <- Some error
    member _.Reset() = value <- None

    interface ISimulatedErrorProvider with
        member _.Find predicate = value |> Option.filter (fun e -> predicate e.Step) |> Option.map _.Error

/// Helpers to ease creating errors from commands
[<AutoOpen>]
module private CmdErrorExtensions =
    type Id<'kind> with
        member this.ToError() = {|
            DuplicateKey = fun () -> Error(DataError(DuplicateKey(Id = this.ToString(), Type = $"%A{this.Kind}")))
            NotFound = fun () -> Error(DataError(DataNotFound(Id = this.ToString(), Type = $"%A{this.Kind}")))
        |}

    type CompensateInvoiceError(cmd: Cmd.CompensateInvoice) =
        member _.InvoiceNotFound() = cmd.InvoiceId.ToError().NotFound()

    type CreateOrderError(cmd: Cmd.CreateOrder) =
        member _.OrderDuplicateKey() = cmd.OrderId.ToError().DuplicateKey()

    type RefundPaymentError(cmd: Cmd.RefundPayment) =
        member _.PaymentNotFound() = cmd.PaymentId.ToError().NotFound()

    type TransitionOrderError(cmd: Cmd.TransitionOrder) =
        member _.NotAllowedFrom(actualStatus: OrderStatus) =
            let reason =
                $"Cannot change order %s{string cmd.OrderId} to %s{cmd.Transition.To.Name}: "
                + $"unexpected current status (expected: %s{cmd.Transition.From.Name}, actual: %s{actualStatus.Name})"

            Error(OperationNotAllowed { Operation = "TransitionOrder"; Reason = reason })

        member _.OrderNotFound() = cmd.OrderId.ToError().NotFound()

    type CmdExtensions =
        [<Extension>]
        static member Error(cmd: Cmd.CompensateInvoice) = CompensateInvoiceError cmd

        [<Extension>]
        static member Error(cmd: Cmd.CreateOrder) = CreateOrderError cmd

        [<Extension>]
        static member Error(cmd: Cmd.RefundPayment) = RefundPaymentError cmd

        [<Extension>]
        static member Error(cmd: Cmd.TransitionOrder) = TransitionOrderError cmd

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

type InvoiceRepository(simulatedErrorProvider: ISimulatedErrorProvider) =
    let invoices = Repository<InvoiceId, Invoice>(_.Id)

    member _.IssueInvoice(cmd: Cmd.IssueInvoice) =
        async {
            match simulatedErrorProvider.Find OrderStep.IssueInvoice with
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

type NotificationClient(simulatedErrorProvider: ISimulatedErrorProvider) =
    member _.SendNotification(cmd: Cmd.NotifyOrderChanged) =
        async {
            match simulatedErrorProvider.Find _.IsSendNotification with
            | Some err -> return Error err
            | None ->
                printfn $"Sending notification for order %A{cmd.OrderId} changed to %A{cmd.NewStatus}"
                return Ok()
        }

type OrderRepository(simulatedErrorProvider: ISimulatedErrorProvider) =
    let orders = Repository<OrderId, Order>(_.Id)

    member _.GetOrderById(orderId) = orders.Get orderId
    member _.DeleteOrder(orderId) = orders.Remove orderId

    member _.CreateOrder(cmd: Cmd.CreateOrder) =
        async {
            match simulatedErrorProvider.Find OrderStep.CreateOrder with
            | Some err -> return Error err
            | None ->
                match! orders.Get cmd.OrderId with
                | None -> return! orders.AddOrUpdate(Order.Create(cmd.Price, cmd.OrderId))
                | Some _ -> return cmd.Error().OrderDuplicateKey()
        }

    member _.TransitionOrder(cmd: Cmd.TransitionOrder) =
        async {
            match! orders.Get cmd.OrderId with
            | Some order when order.Status = cmd.Transition.From -> return! orders.AddOrUpdate { order with Status = cmd.Transition.To }
            | Some order -> return cmd.Error().NotAllowedFrom(order.Status)
            | None -> return cmd.Error().OrderNotFound()
        }

type PaymentRepository(simulatedErrorProvider: ISimulatedErrorProvider) =
    let payments = Repository<PaymentId, Payment>(_.Id)

    member _.ProcessPayment(cmd: Cmd.ProcessPayment) =
        async {
            match simulatedErrorProvider.Find OrderStep.ProcessPayment with
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

type WarehouseClient(simulatedErrorProvider: ISimulatedErrorProvider) =
    member _.ShipOrder(cmd: Cmd.ShipOrder) =
        async {
            match simulatedErrorProvider.Find OrderStep.ShipOrder with
            | Some err -> return Error err
            | None ->
                let parcelId = ParcelId.New()
                printfn $"Shipping order %A{cmd.OrderId} with parcel id %A{parcelId}"
                return Ok parcelId
        }