module Shopfoo.Domain.Types.Warehouse

open System

type EventType =
    | ProductReceived of purchasePrice: decimal<euros>
    | StockAdjusted

type StockEvent = {
    SKU: SKU
    Date: DateOnly
    Quantity: int
    Type: EventType
}