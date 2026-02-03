namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal SaveProductWorkflow private () =
    inherit ProductWorkflow<Product, unit>()
    static member val Instance = SaveProductWorkflow()

    override _.Run product =
        program {
            do! Product.validate product
            do! Program.saveProduct product
            return Ok()
        }