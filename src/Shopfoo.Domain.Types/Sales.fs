module Shopfoo.Domain.Types.Sales

open System

type Prices = {
    SKU: SKU
    RetailPrice: Money

    /// A.k.a. Recommended price
    ListPrice: Money option
}

type Sale = {
    SKU: SKU
    Date: DateOnly
    Price: Money
    Quantity: int
}