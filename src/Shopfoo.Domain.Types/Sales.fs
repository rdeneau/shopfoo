module Shopfoo.Domain.Types.Sales

open System

[<RequireQualifiedAccess>]
type RetailPrice =
    | Regular of price: Money
    | SoldOut

    member this.ToOption() =
        match this with
        | RetailPrice.Regular price -> Some price
        | RetailPrice.SoldOut -> None

type Prices = {
    SKU: SKU
    RetailPrice: RetailPrice

    /// A.k.a. Recommended price
    ListPrice: Money option
}

type Sale = {
    SKU: SKU
    Date: DateOnly
    Price: Money
    Quantity: int
}