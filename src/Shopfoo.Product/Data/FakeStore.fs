/// Fake Store API
/// https://fakestoreapi.com/
module internal Shopfoo.Product.Data.FakeStore

open System
open System.Collections.Concurrent
open System.Net.Http
open FsToolkit.ErrorHandling
open Shopfoo.Common
open Shopfoo.Data.Http
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales

type ProductId = int

[<CLIMutable>]
type ProductDto = {
    Id: ProductId
    Title: string
    Price: decimal
    Description: string
    Category: string
    Image: string
}

type internal FakeStoreClient(httpClient: HttpClient, serializerFactory: HttpApiSerializerFactory) =
    let serializer = serializerFactory.Json(HttpApiName.FakeStore)

    member _.GetProductsAsync() =
        task {
            use request = HttpRequestMessage.Get(Uri.Relative "products")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<ProductDto list>(content)
        }

[<RequireQualifiedAccess>]
module private Mappers =
    module DtoToModel =
        let mapProduct storeProduct (product: ProductDto) : Product = {
            Category = Category.Store storeProduct
            SKU = storeProduct.FSID.AsSKU
            Title = product.Title
            Description = product.Description
            ImageUrl = ImageUrl.Valid product.Image
        }

        let mapStoreCategory (category: string) : StoreCategory option =
            match category.ToLowerInvariant() with
            | "men's clothing"
            | "women's clothing" -> Some StoreCategory.Clothing
            | "electronics" -> Some StoreCategory.Electronics
            | "jewelery" -> Some StoreCategory.Jewelry
            | _ -> None

type private InMemoryProductCache() =
    let cache = ConcurrentDictionary<FSID, Product * Prices>()

    member _.TryGetProduct(fsid) =
        cache.TryGetValue(fsid) |> Option.ofPair |> Option.map fst

    member _.TryGetPrice(fsid) =
        cache.TryGetValue(fsid) |> Option.ofPair |> Option.map snd

    member _.TryGetAllProducts() =
        match cache.Values |> Seq.toList with
        | [] -> None
        | xs -> Some [ for product, _ in xs -> product ]

    member private _.Set(fsid, ?product: Product, ?prices: Prices) =
        result {
            let! entry =
                match product, prices, cache.TryGetValue(fsid) |> Option.ofPair with
                | Some product, Some prices, _ -> Ok(product, prices)
                | Some product, None, Some(_, existingPrices) -> Ok(product, existingPrices)
                | None, Some prices, Some(existingProduct, _) -> Ok(existingProduct, prices)
                | None, None, Some(existingProduct, existingPrices) -> Ok(existingProduct, existingPrices)
                | _, _, None -> Error(GuardClause { EntityName = nameof FSID; ErrorMessage = "Either product or prices must be provided" })

            cache.AddOrUpdate(fsid, entry, fun _ _ -> entry) |> ignore
            return ()
        }

    member private _.WhenFromFakeStore(sku: SKU, f) =
        sku.Match( // ↩
            withFSID = f,
            withISBN = (fun _ -> Error(GuardClause { EntityName = nameof SKU; ErrorMessage = "Expected FSID type" }))
        )

    member this.SetProduct(product: Product) =
        this.WhenFromFakeStore(product.SKU, fun fsid -> this.Set(fsid, product = product))

    member this.SetPrices(prices: Prices) =
        this.WhenFromFakeStore(prices.SKU, fun fsid -> this.Set(fsid, prices = prices))

    member this.SetAll(entries) =
        cache.Clear()

        entries
        |> List.traverseResultM (fun (fsid, product: Product, prices: Prices) ->
            if product.SKU = prices.SKU then
                this.Set(fsid, product, prices)
            else
                Error(GuardClause { EntityName = nameof SKU; ErrorMessage = "Mismatched SKUs between product and prices" })
        )
        |> Result.map ignore

module internal Pipeline =
    let private cache = InMemoryProductCache()

    let getProducts (client: FakeStoreClient) =
        asyncResult {
            match cache.TryGetAllProducts() with
            | Some cachedProducts -> // ↩
                return cachedProducts

            | None ->
                let! productsDtos =
                    task {
                        let! res = client.GetProductsAsync()
                        return res |> liftDataRelatedError
                    }

                let entries = [
                    for productDto in productsDtos do
                        match Mappers.DtoToModel.mapStoreCategory productDto.Category with
                        | None -> ()
                        | Some storeCategory ->
                            let currency =
                                match storeCategory with
                                | StoreCategory.Clothing -> EUR
                                | StoreCategory.Electronics -> USD
                                | StoreCategory.Jewelry -> EUR

                            let storeProduct = { FSID = FSID productDto.Id; Category = storeCategory }
                            let product = Mappers.DtoToModel.mapProduct storeProduct productDto
                            let prices = Prices.Create(product.SKU, currency, retailPrice = productDto.Price)

                            storeProduct.FSID, product, prices
                ]

                do! cache.SetAll entries

                return entries |> List.map (fun (_, product, _) -> product)
        }

    let getPrice sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 100) // Simulate latency
            return cache.TryGetPrice(sku)
        }

    let getProduct sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 150) // Simulate latency
            return cache.TryGetProduct(sku)
        }

    let savePrices prices =
        asyncResult {
            do! Async.Sleep(millisecondsDueTime = 280) // Simulate latency
            do! cache.SetPrices(prices)
        }

    let saveProduct product =
        asyncResult {
            do! Async.Sleep(millisecondsDueTime = 400) // Simulate latency
            do! cache.SetProduct(product)
        }