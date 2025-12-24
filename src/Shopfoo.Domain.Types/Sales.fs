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

/// <summary>
/// The Prices type holds the pricing information for a product identified by its SKU.
/// </summary>
/// <remarks>
/// This model allows currency differences between its fields.
/// In practice, these complex use cases are not covered by the current application.
/// </remarks>
type Prices = {
    SKU: SKU

    /// The currency to use to define the list price and retail price.
    /// In other cases, the prices currency comes from their underlying <c>Money</c> value.
    Currency: Currency

    RetailPrice: RetailPrice

    /// A.k.a. Recommended price
    ListPrice: Money option
} with
    static member Create(sku, currency, retailPrice, ?listPrice) =
        {
            SKU = sku
            Currency = currency
            RetailPrice = RetailPrice.Regular(Money.ByCurrency currency retailPrice)
            ListPrice = listPrice |> Option.map (Money.ByCurrency currency)
        }

type Sale = {
    SKU: SKU
    Date: DateOnly
    Price: Money
    Quantity: int
}