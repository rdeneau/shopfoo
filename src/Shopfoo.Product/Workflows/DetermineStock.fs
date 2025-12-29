namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal DetermineStockWorkflow private () =
    inherit ProductWorkflow<SKU, Stock>()

    override _.Run sku =
        program {
            let! (sales: Sale list) =
                Program.getSales sku
                |> Program.requireSome $"SKU #%s{sku.Value}"
                |> Program.mapDataRelatedError

            let soldQuantity = sales |> Seq.sumBy _.Quantity

            let! stockEvents =
                Program.getStockEvents sku
                |> Program.requireSome $"SKU #%s{sku.Value}"
                |> Program.mapDataRelatedError

            let receivedQuantity =
                (0, stockEvents)
                ||> Seq.fold (fun acc stockEvent ->
                    match stockEvent.Type with
                    | ProductSupplyReceived _ -> acc + stockEvent.Quantity
                    | StockAdjusted -> stockEvent.Quantity
                )

            return Ok { SKU = sku; Quantity = receivedQuantity - soldQuantity }
        }

    static member val Instance = DetermineStockWorkflow()