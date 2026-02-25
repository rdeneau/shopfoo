namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Workflows
open Shopfoo.Program

[<Sealed>]
type internal ReceiveSupplyWorkflow private () =
    static member val Instance = ReceiveSupplyWorkflow()

    interface IProductWorkflow<ReceiveSupplyInput, unit> with
        override _.Run input =
            program {
                do!
                    validation {
                        let! _ = Guard("Quantity").IsPositive(input.Quantity).ToValidation()
                        and! _ = Guard("PurchasePrice").IsPositive(input.PurchasePrice.Value).ToValidation()
                        return ()
                    }

                let stockEvent: StockEvent = {
                    SKU = input.SKU
                    Date = input.Date
                    Quantity = input.Quantity
                    Type = ProductSupplyReceived input.PurchasePrice
                }

                do! Program.addStockEvent stockEvent

                return Ok()
            }