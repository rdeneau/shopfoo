namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal MarkAsSoldOutWorkflow() =
    inherit ProductWorkflow<SKU, unit>()

    override _.Run sku =
        program {
            // TODO: [MarkAsSoldOutWorkflow] get stock and verify it's zero before marking as sold out

            let! prices =
                Program.getPrices sku
                |> Program.requireSome $"SKU #%s{sku.Value}"
                |> Program.mapDataRelatedError

            do! Program.savePrices { prices with RetailPrice = RetailPrice.SoldOut }
            return Ok()
        }