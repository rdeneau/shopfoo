[<RequireQualifiedAccess>]
module Shopfoo.Server.Feat.Catalog

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Products

[<AutoOpen>]
module internal Entities =
    type ProductDto = {
        SKU: string
        Name: string
        Description: string
    }

module internal Mappers =
    module EntitiesToDemain =
        let mapProduct (dto: ProductDto) : Product = {
            SKU = SKU dto.SKU
            Name = dto.Name
            Description = dto.Description
        }

type Api() =
    let products =
        seq {
            {
                SKU = "0321125215"
                Name = "Domain-Driven Design: Tackling Complexity in the Heart of Software"
                Description =
                    "Leading software designers have recognized domain modeling and design as critical topics for at least twenty years, "
                    + "yet surprisingly little has been written about what needs to be done or how to do it. Although it has never been "
                    + "clearly formulated, a philosophy has developed as an undercurrent in the object community, which I call 'domain-driven design'."
                // TODO: [Product] add more books
            }
        }
        |> Seq.map Mappers.EntitiesToDemain.mapProduct
        |> Seq.toList

    member _.GetProducts() : Async<Result<Product list, Error>> =
        async {
            do! Async.Sleep(millisecondsDueTime = 500) // Simulate latency
            return Ok products
        }

    member _.GetProductDetails(sku) : Async<Result<Product option, Error>> =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency
            let product = products |> List.tryFind (fun x -> x.SKU = sku)
            return Ok product
        }