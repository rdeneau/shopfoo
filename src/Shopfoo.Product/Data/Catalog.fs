module Shopfoo.Product.Data.Catalog

open FsToolkit.ErrorHandling
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary

/// <summary>
/// Facade pattern hiding the different data pipelines (<c>Books</c>, <c>FakeStore</c>, <c>OpenLibrary</c>)
/// linked to different product categories and providers.
/// </summary>
type internal CatalogPipeline
    (
        booksPipeline: BooksPipeline, // ↩
        fakeStorePipeline: FakeStorePipeline,
        openLibraryPipeline: OpenLibraryPipeline
    ) =
    member _.GetProducts(provider: Provider) : Async<Product list> =
        async {
            match provider with
            | Provider.OpenLibrary -> // ↩
                return! booksPipeline.GetProducts()

            | Provider.FakeStore ->
                match! fakeStorePipeline.GetProducts() with
                | Ok data -> return data
                | Error _ -> return []
        }

    member _.GetProduct(sku: SKU) : Async<Product option> =
        match sku.Type with
        | SKUType.FSID fsid -> fakeStorePipeline.GetProduct fsid
        | SKUType.ISBN isbn -> booksPipeline.GetProduct isbn
        | SKUType.OLID olid -> openLibraryPipeline.GetProductByOlid olid |> Async.map Result.toOption
        | SKUType.Unknown -> async { return None }

    member _.SaveProduct(product: Product) : Async<Result<unit, Error>> =
        match product.Category with
        | Category.Bazaar _ -> fakeStorePipeline.SaveProduct product
        | Category.Books _ -> booksPipeline.SaveProduct product

    member _.AddProduct(product: Product) : Async<Result<unit, Error>> =
        match product.Category with
        | Category.Books _ -> booksPipeline.AddProduct product
        | Category.Bazaar _ ->
            async {
                let error = GuardClause { EntityName = nameof Product; ErrorMessage = "Adding Bazaar products is not supported" }
                return Error error
            }

    member _.DeleteProduct(sku: SKU) : Async<Result<unit, Error>> =
        match sku.Type with
        | SKUType.ISBN isbn -> booksPipeline.DeleteProduct isbn
        | SKUType.FSID _ -> async { return Error(GuardClause { EntityName = "Product"; ErrorMessage = "Deleting FakeStore products not supported" }) }
        | _ -> async { return Error(DataError(DataNotFound(sku.Value, "Product"))) }