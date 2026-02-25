module Shopfoo.Domain.Types.Warehouse

open System

type EventType =
    | ProductSupplyReceived of purchasePrice: Money
    | StockAdjusted

type StockEvent = {
    SKU: SKU
    Date: DateOnly
    Quantity: int
    Type: EventType
}

module StockEvent =
    let mapPurchasePrice (f: Money -> Money) (stockEvent: StockEvent) : StockEvent =
        match stockEvent.Type with
        | ProductSupplyReceived price -> { stockEvent with Type = ProductSupplyReceived(f price) }
        | StockAdjusted -> stockEvent

type Stock = { SKU: SKU; Quantity: int }

type ReceiveSupplyInput = {
    SKU: SKU
    Date: DateOnly
    Quantity: int
    PurchasePrice: Money
}

type PurchasePrices = {
    /// Price of the most recent `ProductSupplyReceived` event.
    LastPrice: (Money * DateOnly) option
    /// Quantity-weighted average of `ProductSupplyReceived` events within the last 365 days.
    AverageOver1Y: Money option
} with
    static member Empty = { LastPrice = None; AverageOver1Y = None }