namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal AddProductWorkflow private () =
    inherit ProductWorkflow<Product, unit>()

    override _.Run product =
        program {
            do! Product.validate product |> liftGuardClauses
            do! Program.addProduct product
            return Ok()
        }

    static member val Instance = AddProductWorkflow()