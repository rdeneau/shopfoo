namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal SaveProductWorkflow private () =
    inherit ProductWorkflow<Product, unit>()

    override _.Run product =
        program {
            do! Product.validate product |> liftGuardClauses
            do! Program.saveProduct product
            return Ok()
        }

    static member val Instance = SaveProductWorkflow()