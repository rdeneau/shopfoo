namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal RemoveListPriceWorkflow private () =
    inherit ProductWorkflow<SKU, unit>()

    override _.Run sku =
        program {
            let! prices = Program.getPrices sku |> Program.requireSome $"SKU #%s{sku.Value}" |> Program.mapDataRelatedError
            do! Program.savePrices { prices with ListPrice = None }
            return Ok()
        }

    static member val Instance = RemoveListPriceWorkflow()