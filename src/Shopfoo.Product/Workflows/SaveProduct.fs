namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Products
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal SaveProductWorkflow() =
    inherit ProductWorkflow<Product, unit>()

    let validate (product: Product) =
        validation {
            let! _ = Guard(nameof Product.GuardCriteria.SKU).Satisfies(product.SKU.Value, Product.GuardCriteria.SKU).ToValidation()
            and! _ = Guard(nameof Product.GuardCriteria.Name).Satisfies(product.Name, Product.GuardCriteria.Name).ToValidation()
            and! _ = Guard(nameof Product.GuardCriteria.Description).Satisfies(product.Description, Product.GuardCriteria.Description).ToValidation()
            and! _ = Guard(nameof Product.GuardCriteria.ImageUrl).Satisfies(product.ImageUrl, Product.GuardCriteria.ImageUrl).ToValidation()
            return ()
        }

    override _.Run product =
        program {
            do! validate product |> liftGuardClauses
            do! Program.saveProduct product
            return Ok()
        }