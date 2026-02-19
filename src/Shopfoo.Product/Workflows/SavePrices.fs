namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows
open Shopfoo.Program

[<Sealed>]
type internal SavePricesWorkflow private () =
    static member val Instance = SavePricesWorkflow()

    interface IProductWorkflow<Prices, unit> with
        override _.Run prices =
            program {
                do! Prices.validate prices
                let! (PreviousValue _) = Program.savePrices prices
                return Ok()
            }