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

    let getProduct (client: OpenLibrary.OpenLibraryClient) (sku: SKU) =
        match sku.Type with
        | SKUType.FSID fsid -> FakeStore.Pipeline.getProduct fsid
        | SKUType.ISBN isbn -> Books.Pipeline.getProduct isbn
        | SKUType.OLID olid -> OpenLibrary.Pipeline.getProductByOlid client olid |> Async.map Result.toOption
        | SKUType.Unknown -> Async.retn None

    let saveProduct (product: Product) =
        match product.Category, product.SKU.Type with
        | Category.Bazaar _, SKUType.FSID _ -> FakeStore.Pipeline.saveProduct product
        | Category.Bazaar _, _ -> failwith $"Cannot save a Bazaar product with the SKU type {product.SKU.Type}."
        | Category.Books _, SKUType.ISBN _ -> Books.Pipeline.saveProduct product
        | Category.Books _, _ -> failwith $"Cannot save a book with the SKU type {product.SKU.Type}."

    let addProduct (product: Product) =
        match product.Category, product.SKU.Type with
        | Category.Books _, SKUType.OLID _ -> Books.Pipeline.addProduct product
        | Category.Books _, _ -> failwith $"Cannot add a book with the SKU type {product.SKU.Type}."
        | Category.Bazaar _, _ -> failwith $"Cannot save a Bazaar product with the SKU type {product.SKU.Type}."