namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
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

            let initialPrices = {
                SKU = sku
                Currency = currency
                ListPrice = None
                RetailPrice = RetailPrice.SoldOut
            }

            // TODO RDE: handle addProduct and addPrices in Parallel
            // TODO RDE: handle addProduct and addPrices in a "saga", with compensation actions in case of failure (e.g. if addPrices fails, remove the product that was just added)
            do! Program.addProduct product
            do! Program.addPrices initialPrices

            return Ok()
        }