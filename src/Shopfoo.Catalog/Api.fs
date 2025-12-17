namespace Shopfoo.Catalog

open Shopfoo.Catalog.Data
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Products

[<Interface>]
type ICatalogApi =
    abstract member GetProducts: unit -> Async<Result<Product list, Error>>
    abstract member GetProduct: sku: SKU -> Async<Result<Product option, Error>>
    abstract member SaveProduct: product: Product -> Async<Result<unit, Error>>

type internal Api() =
    interface ICatalogApi with
        member _.GetProducts() =
            async {
                do! Async.Sleep(millisecondsDueTime = 500) // Simulate latency
                return Ok(Products.repository.Values |> Seq.toList)
            }

        member _.GetProduct(sku) =
            async {
                do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency
                let product = Products.repository.Values |> Seq.tryFind (fun x -> x.SKU = sku)
                return Ok product
            }

        member _.SaveProduct(product) =
            async {
                do! Async.Sleep(millisecondsDueTime = 400) // Simulate latency

                if Products.repository.ContainsKey(product.SKU) then
                    Products.repository[product.SKU] <- product
                    return Ok()
                else
                    return Error(DataError(DataNotFound(Id = product.SKU.Value, Type = nameof Product)))
            }

module DependencyInjection =
    open Microsoft.Extensions.DependencyInjection

    type IServiceCollection with
        member services.AddCatalogApi() = // ↩
            services.AddSingleton<ICatalogApi, Api>()