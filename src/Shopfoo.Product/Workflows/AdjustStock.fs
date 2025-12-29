namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal AdjustStockWorkflow private () =
    inherit ProductWorkflow<Stock, unit>()

    override _.Run stock =
        program {
            do! Program.adjustStock stock
            return Ok()
        }

    static member val Instance = AdjustStockWorkflow()