[<RequireQualifiedAccess>]
module internal Shopfoo.Catalog.Data.Sales

open System.Linq
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
                    1 |> unitsSold (Dollars 38.99m) (342 |> daysAgo)
                    2 |> unitsSold (Dollars 38.99m) (231 |> daysAgo)
                    1 |> unitsSold (Dollars 39.90m) (120 |> daysAgo)
                ]
            yield!
                SKU.DomainDrivenDesign.Sales [
                    1 |> unitsSold (67.33m |> Euros) (289 |> daysAgo)
                    1 |> unitsSold (64.42m |> Euros) (178 |> daysAgo)
                    3 |> unitsSold (64.42m |> Euros) (131 |> daysAgo)
                ]
            yield!
                SKU.DomainModelingMadeFunctional.Sales [
                    4 |> unitsSold (43.04m |> Euros) (336 |> daysAgo)
                    3 |> unitsSold (43.04m |> Euros) (225 |> daysAgo)
                    5 |> unitsSold (32.32m |> Euros) (114 |> daysAgo)
                    8 |> unitsSold (32.32m |> Euros) (045 |> daysAgo)
                ]
            yield!
                SKU.JavaScriptTheGoodParts.Sales [ // ↩
                    1 |> unitsSold (28.92m |> Euros) (365 |> daysAgo)
                    2 |> unitsSold (19.98m |> Euros) (112 |> daysAgo)
                ]
            yield!
                SKU.ThePragmaticProgrammer.Sales [ // ↩
                    1 |> unitsSold (49.37m |> Euros) (364 |> daysAgo)
                    3 |> unitsSold (32.86m |> Euros) (119 |> daysAgo)
                ]
        ]

module Client =
    let repository =
        Fakes.oneYear
        |> Seq.groupBy _.SKU
        |> Seq.map (fun (sku, sales) -> sku, sales |> Seq.toList)
        |> _.ToDictionary(fst, snd)

    let getSales sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency
            return repository.TryGetValue(sku) |> Option.ofPair
        }