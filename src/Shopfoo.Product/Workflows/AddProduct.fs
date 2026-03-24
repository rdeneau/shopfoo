namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal AddProductWorkflow private () =
    inherit ProductWorkflow<Product * Currency, unit>()
    static member val Instance = AddProductWorkflow()

    override _.Run((product, currency)) =
        program {
            let sku =
                match product.SKU.Type, product.Category with
                | SKUType.OLID _, Category.Books book -> book.ISBN.AsSKU
                | _ -> product.SKU

            let product = { product with SKU = sku }

            do! Product.validate product

            // addProduct and addPrices can be run in Parallel
            let! resA = Program.addProduct product
            and! resB = Program.addPrices (Prices.Initial(sku, currency))

            // Combine potential errors from any or both operations, and ignore the successful values (both `unit`)
            return Result.zip resA resB |> Result.map ignore
        }