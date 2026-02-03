namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects
open Shopfoo.Product.Model
open Shopfoo.Product.Workflows.Instructions

[<Sealed>]
type internal AddProductWorkflow private () =
    inherit ProductWorkflow<Product, unit>()

    override _.Run product =
        program {
            let sku =
                match product.SKU.Type, product.Category with
                | SKUType.OLID _, Category.Books book -> book.ISBN.AsSKU
                | _ -> product.SKU

            let product = { product with SKU = sku }

            do! Product.validate product
            do! Program.addProduct product

            let initialPrices = {
                SKU = sku
                Currency = Currency.EUR
                ListPrice = None
                RetailPrice = RetailPrice.SoldOut
            }

            do! Program.addPrices initialPrices

            return Ok()
        }

    static member val Instance = AddProductWorkflow()