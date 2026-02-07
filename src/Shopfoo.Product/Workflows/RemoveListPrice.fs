namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Workflows
open Shopfoo.Program

[<Sealed>]
type internal RemoveListPriceWorkflow private () =
    static member val Instance = RemoveListPriceWorkflow()

    interface IProductWorkflow<SKU, unit> with
        override _.Run sku =
            program {
                let! prices = Program.getPrices sku |> Program.requireSomeData ($"SKU #%s{sku.Value}", TypeName.Custom "Prices")
                do! Program.savePrices { prices with ListPrice = None }
                return Ok()
            }