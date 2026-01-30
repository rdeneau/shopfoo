namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Effects
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal AddProductWorkflow private () =
    inherit ProductWorkflow<Product, unit>()

    let validate (product: Product) =
        validation {
            let! _ = Guard(nameof Product.Guard.SKU).Satisfies(product.SKU.Value, Product.Guard.SKU).ToValidation()
            and! _ = Guard(nameof Product.Guard.Name).Satisfies(product.Title, Product.Guard.Name).ToValidation()
            and! _ = Guard(nameof Product.Guard.Description).Satisfies(product.Description, Product.Guard.Description).ToValidation()
            and! _ = Guard(nameof Product.Guard.ImageUrl).Satisfies(product.ImageUrl.Url, Product.Guard.ImageUrl).ToValidation()
            return ()
        }

    override _.Run product =
        program {
            do! validate product |> liftGuardClauses
            do! Program.addProduct product
            return Ok()
        }

    static member val Instance = AddProductWorkflow()