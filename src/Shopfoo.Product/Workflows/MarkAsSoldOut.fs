namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows
open Shopfoo.Program

[<Sealed>]
type internal MarkAsSoldOutWorkflow private (determineStockWorkflow: DetermineStockWorkflow) =
    static member val Instance = MarkAsSoldOutWorkflow(DetermineStockWorkflow.Instance)

    interface IProductWorkflow<SKU, unit> with
        override _.Run sku =
            program {
                let! stock = (determineStockWorkflow :> IProductWorkflow<_, _>).Run sku
                do! Stock.verifyNoStock stock

                let! prices = Program.getPrices sku |> Program.requireSomeData ($"SKU #%s{sku.Value}", TypeName.Custom "Prices")
                do! Program.savePrices { prices with RetailPrice = RetailPrice.SoldOut }

                return Ok()
            }