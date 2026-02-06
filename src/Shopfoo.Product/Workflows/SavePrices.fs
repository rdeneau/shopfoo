namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows

[<Sealed>]
type internal SavePricesWorkflow private () =
    static member val Instance = SavePricesWorkflow()

    interface IProductWorkflow<Prices, unit> with
        override _.Run prices =
            program {
                do! Prices.validate prices
                do! Program.savePrices prices
                return Ok()
            }