/// Fake Store API
/// https://fakestoreapi.com/
module Shopfoo.Product.Data.FakeStore

open System
open System.Net.Http
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Shopfoo.Data.Http
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales

[<AutoOpen>]
module Dto =
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

[<Interface>]
type IFakeStoreClient =
    abstract member GetProductsAsync: unit -> Task<Result<ProductDto list, DataRelatedError>>

[<RequireQualifiedAccess>]
module private Mappers =
    module DtoToModel =
        let mapProduct storeProduct (product: ProductDto) : Product = {
            Category = Category.Bazaar storeProduct
            SKU = storeProduct.FSID.AsSKU
            Title = product.Title
            Description = product.Description
            ImageUrl = ImageUrl.Valid product.Image
        }

        let mapStoreCategory (category: string) : BazaarCategory option =
            match category.ToLowerInvariant() with
            | "men's clothing"
            | "women's clothing" -> Some BazaarCategory.Clothing
            | "electronics" -> Some BazaarCategory.Electronics
            | "jewelery" -> Some BazaarCategory.Jewelry
            | _ -> None

type internal FakeStorePipeline(fakeStoreClient: IFakeStoreClient) =
    let cache = InMemoryProductCache()

    member _.GetProducts() : Async<Result<Product list, Error>> =
        asyncResult {
            match cache.TryGetAllProducts() with
            | Some cachedProducts -> // ↩
                return cachedProducts

            | None ->
                let! productsDtos =
                    task {
                        let! res = fakeStoreClient.GetProductsAsync()
                        return res |> liftDataRelatedError
                    }

                let entries = [
                    for productDto in productsDtos do
                        match Mappers.DtoToModel.mapStoreCategory productDto.Category with
                        | None -> ()
                        | Some storeCategory ->
                            let currency =
                                match storeCategory with
                                | BazaarCategory.Clothing -> EUR
                                | BazaarCategory.Electronics -> USD
                                | BazaarCategory.Jewelry -> EUR

                            let storeProduct = { FSID = FSID productDto.Id; Category = storeCategory }
                            let product = Mappers.DtoToModel.mapProduct storeProduct productDto
                            let prices = Prices.Create(product.SKU, currency, retailPrice = productDto.Price)

                            storeProduct.FSID, product, prices
                ]

                do! cache.SetAll entries

                return entries |> List.map (fun (_, product, _) -> product)
        }

    member _.GetPrice(fsid: FSID) : Async<Prices option> =
        async {
            do! Fake.latencyInMilliseconds 100
            return cache.TryGetPrice(fsid)
        }

    member _.GetProduct(fsid: FSID) : Async<Product option> =
        async {
            do! Fake.latencyInMilliseconds 150
            return cache.TryGetProduct(fsid)
        }

    member _.SavePrices(prices: Prices) : Async<Result<unit, Error>> =
        asyncResult {
            do! Fake.latencyInMilliseconds 280
            do! cache.SetPrices(prices)
        }

    member _.SaveProduct(product: Product) : Async<Result<unit, Error>> =
        asyncResult {
            do! Fake.latencyInMilliseconds 400
            do! cache.SetProduct(product)
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

    interface IFakeStoreClient with
        member this.GetProductsAsync() = this.GetProductsAsync()