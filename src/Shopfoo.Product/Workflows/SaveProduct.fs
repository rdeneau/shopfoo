namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows

[<Sealed>]
type internal SaveProductWorkflow private () =
    static member val Instance = SaveProductWorkflow()

    interface IProductWorkflow<Product, unit> with
        override _.Run product =
            program {
                do! Product.validate product
                do! Program.saveProduct product
                return Ok()
            }