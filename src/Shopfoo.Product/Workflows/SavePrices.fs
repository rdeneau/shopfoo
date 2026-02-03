namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal SavePricesWorkflow private () =
    inherit ProductWorkflow<Prices, unit>()

    override _.Run prices =
        program {
            do! Prices.validate prices
            do! Program.savePrices prices
            return Ok()
        }

    static member val Instance = SavePricesWorkflow()