module Shopfoo.Domain.Types.Warehouse

open System

type EventType =
    | ProductReceived of purchasePrice: Money
    | StockAdjusted

type StockEvent = {
    SKU: SKU
    Date: DateOnly
    Quantity: int
    Type: EventType
}