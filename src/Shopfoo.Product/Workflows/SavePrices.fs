namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal SavePricesWorkflow() =
    inherit ProductWorkflow<Prices, unit>()

    let validate (prices: Prices) =
        validation {
            let recommendedPrice = prices.ListPrice |> Option.map _.Value |> Option.defaultValue 0m
            let retailPrice = prices.RetailPrice.Value
            let! _ = Guard(nameof recommendedPrice).IsPositiveOrZero(recommendedPrice).ToValidation()
            and! _ = Guard(nameof retailPrice).IsPositive(retailPrice).ToValidation()
            return ()
        }

    override _.Run prices =
        program {
            do! validate prices |> liftGuardClauses
            do! Program.savePrices prices
            return Ok()
        }