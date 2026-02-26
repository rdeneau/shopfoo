module Shopfoo.Product.Data.Sales

open System
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data
open Shopfoo.Product.Data.Helpers

type SalesRepository(sales: Sale seq) =
    let repository = FakeRepository(sales, _.SKU)

    member _.AddSale(sale: Sale) : unit = repository.Add sale
    member _.GetSales(sku: SKU) : Sale list option = repository.Get sku

type internal SalesPipeline(repository: SalesRepository) =
    member _.GetSales(sku: SKU) : Async<Sale list option> =
        async {
            do! Fake.latencyInMilliseconds 250
            return repository.GetSales sku
        }

    member _.GetSalesStats(sku: SKU) : Async<SalesStats> =
        async {
            do! Fake.latencyInMilliseconds 200

            let sales = repository.GetSales sku |> Option.defaultValue []

            match sales with
            | [] -> return SalesStats.Empty
            | _ ->
                let lastSale = sales |> List.maxBy _.Date

                let cutoff = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays -365)
                let within1Y = sales |> List.filter (fun s -> s.Date >= cutoff)
                let totalQty = within1Y |> List.sumBy _.Quantity

                let currency = within1Y |> Seq.map (fun s -> s.Price.Currency) |> Seq.distinct |> Seq.tryExactlyOne

                let totalOver1Y =
                    match currency with
                    | Some currency when totalQty > 0 ->
                        let totalAmount = within1Y |> List.sumBy (fun s -> s.Price.Value * decimal s.Quantity)
                        Some(Money.ByCurrency currency totalAmount)
                    | _ -> None

                return {
                    LastSale = Some lastSale
                    QuantityOver1Y = totalQty
                    TotalOver1Y = totalOver1Y
                }
        }

    member _.AddSale(sale: Sale) : Async<Result<unit, 'a>> =
        async {
            do! Fake.latencyInMilliseconds 250
            repository.AddSale sale
            return Ok()
        }

module private Fakes =
    let oneYear = [
        yield!
            ISBN.CleanArchitecture.Sales [
                1 |> unitsSold (Dollars 38.99m) (342 |> daysAgo)
                2 |> unitsSold (Dollars 38.99m) (231 |> daysAgo)
                1 |> unitsSold (Dollars 39.90m) (120 |> daysAgo)
            ]
        yield!
            ISBN.DomainDrivenDesign.Sales [
                1 |> unitsSold (67.33m |> Euros) (289 |> daysAgo)
                1 |> unitsSold (64.42m |> Euros) (178 |> daysAgo)
                3 |> unitsSold (64.42m |> Euros) (131 |> daysAgo)
            ]
        yield!
            ISBN.DomainModelingMadeFunctional.Sales [
                4 |> unitsSold (43.04m |> Euros) (336 |> daysAgo)
                3 |> unitsSold (43.04m |> Euros) (225 |> daysAgo)
                5 |> unitsSold (32.32m |> Euros) (114 |> daysAgo)
                8 |> unitsSold (32.32m |> Euros) (045 |> daysAgo)
            ]
        yield!
            ISBN.JavaScriptTheGoodParts.Sales [ // ↩
                1 |> unitsSold (28.92m |> Euros) (365 |> daysAgo)
                2 |> unitsSold (19.98m |> Euros) (112 |> daysAgo)
            ]
        yield!
            ISBN.ThePragmaticProgrammer.Sales [ // ↩
                1 |> unitsSold (49.37m |> Euros) (364 |> daysAgo)
                3 |> unitsSold (32.86m |> Euros) (119 |> daysAgo)
            ]
    ]

[<RequireQualifiedAccess>]
module SalesRepository =
    let internal instance = SalesRepository Fakes.oneYear