namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal SavePricesWorkflow() =
    inherit ProductWorkflow<Prices, unit>()

    let guardListPrice (prices: Prices) =
        match prices.ListPrice with
        | Some price -> Guard(nameof prices.ListPrice).IsPositive(price.Value)
        | None -> Ok 0m

    let guardRetailPrice (prices: Prices) =
        match prices.RetailPrice with
        | RetailPrice.Regular price -> Guard(nameof prices.RetailPrice).IsPositive(price.Value)
        | RetailPrice.SoldOut -> Ok 0m

    let validate (prices: Prices) =
        validation {
            let! _ = guardListPrice(prices).ToValidation()
            and! _ = guardRetailPrice(prices).ToValidation()
            return ()
        }

    override _.Run prices =
        program {
            do! validate prices |> liftGuardClauses
            do! Program.savePrices prices
            return Ok()
        }