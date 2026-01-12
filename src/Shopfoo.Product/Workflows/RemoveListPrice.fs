namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal RemoveListPriceWorkflow private () =
    inherit ProductWorkflow<SKU, unit>()

    override _.Run sku =
        program {
            let! prices =
                Program.getPrices sku
                |> Program.requireSomeData ($"SKU #%s{sku.Value}", TypeName.Custom "Prices")
                |> Program.mapDataRelatedError

            do! Program.savePrices { prices with ListPrice = None }
            return Ok()
        }

    static member val Instance = RemoveListPriceWorkflow()