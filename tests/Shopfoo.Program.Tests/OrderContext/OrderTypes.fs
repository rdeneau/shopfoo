namespace Shopfoo.Product.Tests.OrderContext

open System
open Shopfoo.Domain.Types.Errors

// TODO RDE: Document this pattern for creating strongly-typed IDs:
//   ✅ reduced boilerplate
//   ✅ terse formatting by fantomas, contrary to unit of measure needing 2 more lines - see https://nordfjord.io/equinox/ using FSharp.UMX
//   ✅ standalone: no need for external dependencies like FSharp.UMX
//   ✅ type safety
//   ⚠️ recursive module: optional, trade-off
//       ✅ optional: just to use the type aliases in the XxxId.New function whereas they are declared afterwards
//       ✅ to secure when using Id.New : the type annotation using the type alias prevents accidentally forgetting to specify the prefix
//       ⚠️ can be confusing
//   ⚠️ the 'kind refers to the entity, not the ID (as it's traditionally done in other patterns), hence the Type property and the %A format to get the case name!
//       tradeoff: terse vs being more explicit constraint on the 'kind generic type
[<AutoOpen>]
module rec Id =
    type Id<'kind> = private {
        Kind: 'kind
        Prefix: string
        Id: Guid
    } with
        member this.Type = $"%A{this.Kind}"
        member this.Value = $"%s{this.Prefix}-%s{this.Id.ToString()[0..7]}"

        static member New (kind: 'kind) prefix : Id<'kind> = {
            Kind = kind
            Prefix = prefix
            Id = Guid.NewGuid()
        }

    module InvoiceId =
        type Invoice = private | Invoice
        let New () : InvoiceId = Id.New Invoice "INV"

    module OrderId =
        type Order = private | Order
        let New () : OrderId = Id.New Order "ORD"

    module ParcelId =
        type Parcel = private | Parcel
        let New () : ParcelId = Id.New Parcel "PAR"

    module PaymentId =
        type Payment = private | Payment
        let New () : PaymentId = Id.New Payment "PAY"

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