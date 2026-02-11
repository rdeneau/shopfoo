namespace Shopfoo.Product.Tests.OrderContext

open System

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