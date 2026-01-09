[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Catalog

open FsToolkit.ErrorHandling
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Data

/// Facade design pattern hiding the different data sources: Books and FakeStore
module Pipeline =
    let getProducts fakeStoreClient provider =
        async {
            match provider with
            | OpenLibrary -> // ↩
                return! Books.Pipeline.getProducts ()

            | FakeStore ->
                match! FakeStore.Pipeline.getProducts fakeStoreClient with
                | Ok data -> return data
                | Error _ -> return []
        }

    let getProduct (sku: SKU) =
        match sku.Type with
        | SKUType.FSID fsid -> FakeStore.Pipeline.getProduct fsid
        | SKUType.ISBN isbn -> Books.Pipeline.getProduct isbn
        | SKUType.Unknown -> Async.retn None

    let saveProduct (product: Product) =
        match product.Category with
        | Category.Books _ -> Books.Pipeline.saveProduct product
        | Category.Store _ -> FakeStore.Pipeline.saveProduct product