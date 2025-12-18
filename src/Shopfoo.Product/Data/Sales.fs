[<RequireQualifiedAccess>]
module internal Shopfoo.Catalog.Data.Sales

open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data

module private Fakes =
    let private unitsSold price date quantity : Sale = {
        SKU = SKU.none
        Date = date
        Price = price
        Quantity = quantity
    }

    type SKU with
        member sku.Sales(sales: Sale seq) = [ for sale in sales -> { sale with SKU = sku } ]

    let oneYear =
        ResizeArray [
            yield!
                SKU.CleanArchitecture.Sales [
                    1 |> unitsSold 38.99m<euros> (342 |> daysAgo)
                    2 |> unitsSold 38.99m<euros> (231 |> daysAgo)
                    1 |> unitsSold 39.90m<euros> (120 |> daysAgo)
                ]
            yield!
                SKU.DomainDrivenDesign.Sales [
                    1 |> unitsSold 67.33m<euros> (289 |> daysAgo)
                    1 |> unitsSold 64.42m<euros> (178 |> daysAgo)
                    3 |> unitsSold 64.42m<euros> (131 |> daysAgo)
                ]
            yield!
                SKU.DomainModelingMadeFunctional.Sales [
                    4 |> unitsSold 43.04m<euros> (336 |> daysAgo)
                    3 |> unitsSold 43.04m<euros> (225 |> daysAgo)
                    5 |> unitsSold 32.32m<euros> (114 |> daysAgo)
                    8 |> unitsSold 32.32m<euros> (045 |> daysAgo)
                ]
            yield!
                SKU.JavaScriptTheGoodParts.Sales [ // ↩
                    1 |> unitsSold 28.92m<euros> (365 |> daysAgo)
                    2 |> unitsSold 19.98m<euros> (112 |> daysAgo)
                ]
            yield!
                SKU.ThePragmaticProgrammer.Sales [ // ↩
                    1 |> unitsSold 49.37m<euros> (364 |> daysAgo)
                    3 |> unitsSold 32.86m<euros> (119 |> daysAgo)
                ]
        ]

module Client =
    let repository = Fakes.oneYear |> dictionaryBy _.SKU

    let getSales sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency
            let sales = Fakes.oneYear |> Seq.filter (fun x -> x.SKU = sku) |> Seq.toList
            return Ok sales
        }
