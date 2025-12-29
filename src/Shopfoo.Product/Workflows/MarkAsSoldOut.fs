namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal MarkAsSoldOutWorkflow private (determineStockWorkflow: DetermineStockWorkflow) =
    inherit ProductWorkflow<SKU, unit>()

    let verifyZeroStock (stock: Stock) =
        validation {
            let! _ =
                Guard(nameof Stock) // ↩
                    .Satisfies(stock.Quantity = 0, "Stock quantity must be zero to mark as sold out.")
                    .ToValidation()

            return ()
        }

    override _.Run sku =
        program {
            let! stock = determineStockWorkflow.Run sku
            do! verifyZeroStock stock |> liftGuardClauses

            let! prices =
                Program.getPrices sku
                |> Program.requireSome $"SKU #%s{sku.Value}"
                |> Program.mapDataRelatedError

            do! Program.savePrices { prices with RetailPrice = RetailPrice.SoldOut }
            return Ok()
        }

    static member val Instance = MarkAsSoldOutWorkflow(DetermineStockWorkflow.Instance)