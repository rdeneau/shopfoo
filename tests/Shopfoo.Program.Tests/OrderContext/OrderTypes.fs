namespace Shopfoo.Product.Tests.OrderContext

open System
open Shopfoo.Domain.Types.Errors

[<AutoOpen>]
module Id =
    type Id<'kind> = {
        Kind: 'kind
        Value: string
    } with
        override this.ToString() = $"%A{this.Kind}-%s{this.Value}"

    module Id =
        let New kind = { Kind = kind; Value = Guid.NewGuid().ToString()[0..7] }

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

type OrderStatus =
    | OrderCancelled of previous: OrderStatus
    | OrderCreated
    | OrderInvoiced of InvoiceId
    | OrderPaid of PaymentId
    | OrderShipped of ParcelId

    member this.Name =
        match this with
        | OrderCancelled _ -> "Cancelled"
        | OrderCreated -> "Created"
        | OrderInvoiced _ -> "Invoiced"
        | OrderPaid _ -> "Paid"
        | OrderShipped _ -> "Shipped"

type Order = {
    Id: OrderId
    Price: decimal
    Status: OrderStatus
} with
    static member Create(price, ?id) = {
        Id = defaultArg id (OrderId.New())
        Price = price
        Status = OrderCreated
    }

type OrderError =
    | OrderCannotBeCancelledAfterShipping
    | OrderTransitionForbidden of current: OrderStatus * attempted: OrderStatus

    interface IBusinessError with
        override this.Code =
            match this with
            | OrderCannotBeCancelledAfterShipping -> "OrderCannotBeCancelledAfterShipping"
            | OrderTransitionForbidden _ -> "OrderTransitionForbidden"

        override this.Message =
            match this with
            | OrderCannotBeCancelledAfterShipping -> "Order cannot be cancelled after it has been shipped."
            | OrderTransitionForbidden(current, attempted) -> $"Transition from %s{current.Name} to %s{attempted.Name} is not allowed."

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

type Transition = { From: OrderStatus; To: OrderStatus }

module Cmd =
    type CreateOrder = { OrderId: OrderId; Price: decimal }
    type NotifyOrderChanged = { OrderId: OrderId; NewStatus: OrderStatus }
    type IssueInvoice = { OrderId: OrderId; Amount: decimal }
    type ProcessPayment = { OrderId: OrderId; Amount: decimal }
    type ShipOrder = { OrderId: OrderId }
    type TransitionOrder = { OrderId: OrderId; Transition: Transition }

    // Undo commands
    type CompensateInvoice = { InvoiceId: InvoiceId }
    type RefundPayment = { PaymentId: PaymentId }

[<AutoOpen>]
module Extensions =
    type Transition with
        member this.Revert() = { this with From = this.To; To = this.From }

    type Cmd.TransitionOrder with
        member this.Revert() = { this with Transition = this.Transition.Revert() }