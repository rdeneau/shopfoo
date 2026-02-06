[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Catalog

open FsToolkit.ErrorHandling
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Data

/// <summary>
/// Facade pattern hiding the different data sources: <c>Books</c>, <c>OpenLibrary</c>, <c>FakeStore</c>
/// </summary>
module Pipeline =
    let getProducts fakeStoreClient provider =
        async {
            match provider with
            | Provider.OpenLibrary -> // ↩
                return! Books.Pipeline.getProducts ()

            | Provider.FakeStore ->
                match! FakeStore.Pipeline.getProducts (fakeStoreClient :> FakeStore.IFakeStoreClient) with
                | Ok data -> return data
                | Error _ -> return []
        }

    let getProduct (client: OpenLibrary.IOpenLibraryClient) (sku: SKU) =
        match sku.Type with
        | SKUType.FSID fsid -> FakeStore.Pipeline.getProduct fsid
        | SKUType.ISBN isbn -> Books.Pipeline.getProduct isbn
        | SKUType.OLID olid -> OpenLibrary.Pipeline.getProductByOlid client olid |> Async.map Result.toOption
        | SKUType.Unknown -> async { return None }

    let saveProduct (product: Product) =
        match product.Category with
        | Category.Bazaar _ -> FakeStore.Pipeline.saveProduct product
        | Category.Books _ -> Books.Pipeline.saveProduct product

    let addProduct (product: Product) =
        match product.Category with
        | Category.Books _ -> Books.Pipeline.addProduct product
        | Category.Bazaar _ ->
            async { return Error(GuardClause { EntityName = nameof Product; ErrorMessage = "Adding Bazaar products is not supported" }) }