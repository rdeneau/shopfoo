[<RequireQualifiedAccess>]
module Shopfoo.Product.Model.Prices

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales

let private guardListPrice (prices: Prices) =
    match prices.ListPrice with
    | Some price -> Guard(nameof prices.ListPrice).IsPositive(price.Value)
    | None -> Ok 0m

let private guardRetailPrice (prices: Prices) =
    match prices.RetailPrice with
    | RetailPrice.Regular price -> Guard(nameof prices.RetailPrice).IsPositive(price.Value)
    | RetailPrice.SoldOut -> Ok 0m

let validate (prices: Prices) =
    validation {
        let! _ = guardListPrice(prices).ToValidation()
        and! _ = guardRetailPrice(prices).ToValidation()
        return ()
    }