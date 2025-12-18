module Shopfoo.Domain.Types.Sales

open System

type Prices = {
    SKU: SKU
    RetailPrice: decimal<euros>
    RecommendedPrice: decimal<euros> option
}

type Sale = {
    SKU: SKU
    Date: DateOnly
    Price: decimal<euros>
    Quantity: int
}