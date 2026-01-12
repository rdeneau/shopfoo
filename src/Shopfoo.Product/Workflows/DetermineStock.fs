namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<RequireQualifiedAccess>]
type private StockEventType =
    | Shipped
    | SupplyReceived
    | StockAdjusted

[<Sealed>]
type internal DetermineStockWorkflow private () =
    inherit ProductWorkflow<SKU, Stock>()

    override _.Run sku =
        program {
            let! (sales: Sale list) =
                Program.getSales sku // ↩
                |> Program.requireSomeData ($"SKU #%s{sku.Value}", TypeName.Custom "Sales")
                |> Program.mapDataRelatedError

            let! stockEvents =
                Program.getStockEvents sku
                |> Program.requireSomeData ($"SKU #%s{sku.Value}", TypeName.Custom "Stock")
                |> Program.mapDataRelatedError

            let allEvents =
                [
                    for sale in sales do
                        StockEventType.Shipped, sale.Date, sale.Quantity

                    for stockEvent in stockEvents do
                        match stockEvent.Type with
                        | ProductSupplyReceived _ -> StockEventType.SupplyReceived, stockEvent.Date, stockEvent.Quantity
                        | StockAdjusted -> StockEventType.StockAdjusted, stockEvent.Date, stockEvent.Quantity
                ]
                |> List.sortBy (fun (_, date, _) -> date)

            let quantity =
                (0, allEvents)
                ||> Seq.fold (fun acc (eventType, _, quantity) ->
                    match eventType with
                    | StockEventType.Shipped -> acc - quantity
                    | StockEventType.SupplyReceived -> acc + quantity
                    | StockEventType.StockAdjusted -> quantity
                )

            return Ok { SKU = sku; Quantity = quantity }
        }

    static member val Instance = DetermineStockWorkflow()