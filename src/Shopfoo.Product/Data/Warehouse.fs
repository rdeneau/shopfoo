module Shopfoo.Product.Data.Warehouse

open System
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data
open Shopfoo.Product.Data.Helpers

type StockEventRepository(stockEvents: StockEvent seq) =
    let repository = FakeRepository(stockEvents, _.SKU)

    member _.AddStockEvent(stockEvent: StockEvent) : unit = repository.Add stockEvent
    member _.GetStockEvents(sku: SKU) : StockEvent list option = repository.Get sku

type internal WarehousePipeline(repository: StockEventRepository) =
    member _.AdjustStock({ SKU = sku; Quantity = quantity }: Stock) : Async<Result<unit, 'a>> =
        async {
            do! Fake.latencyInMilliseconds 250

            repository.AddStockEvent {
                SKU = sku
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
                Quantity = quantity
                Type = EventType.StockAdjusted
            }

            return Ok()
        }

    member _.AddStockEvent(stockEvent: StockEvent) : Async<Result<unit, 'a>> =
        async {
            do! Fake.latencyInMilliseconds 250
            repository.AddStockEvent stockEvent
            return Ok()
        }

    member _.GetStockEvents(sku: SKU) : Async<StockEvent list option> =
        async {
            do! Fake.latencyInMilliseconds 150
            return repository.GetStockEvents sku
        }

    member _.GetPurchasePrices(sku: SKU) : Async<PurchasePrices> =
        async {
            do! Fake.latencyInMilliseconds 200

            let stockEvents = repository.GetStockEvents sku |> Option.defaultValue []

            let purchaseEvents =
                stockEvents
                |> List.choose (fun e ->
                    match e.Type with
                    | EventType.ProductSupplyReceived price -> Some(e.Date, price, decimal e.Quantity)
                    | _ -> None
                )

            match purchaseEvents with
            | [] -> return PurchasePrices.Empty
            | _ ->
                let lastDate, lastPrice, _ = purchaseEvents |> List.maxBy (fun (date, _, _) -> date)

                let cutoff = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays -365)
                let within1Y = purchaseEvents |> List.filter (fun (date, _, _) -> date >= cutoff)
                let currency = within1Y |> Seq.map (fun (_, price, _) -> price.Currency) |> Seq.distinct |> Seq.tryExactlyOne
                let totalQty = within1Y |> List.sumBy (fun (_, _, qty) -> qty)

                let averageOver1Y =
                    match currency with
                    | Some currency when totalQty > 0m ->
                        let totalAmount = within1Y |> List.sumBy (fun (_, price, qty) -> price.Value * qty)
                        Some(Money.ByCurrency currency (totalAmount / totalQty))
                    | _ -> None

                return { LastPrice = Some(lastPrice, lastDate); AverageOver1Y = averageOver1Y }
        }

module private Fakes =
    let oneYear = [
        yield!
            ISBN.CleanArchitecture.Events [ // ↩
                2 |> Units.Purchased (Dollars 23.99m) (365 |> daysAgo)
                8 |> Units.Purchased (Dollars 24.99m) (360 |> daysAgo)
            ]
        yield!
            ISBN.DomainDrivenDesign.Events [ // ↩
                8 |> Units.Purchased (44.30m |> Euros) (365 |> daysAgo)
            ]
        yield!
            ISBN.DomainModelingMadeFunctional.Events [
                15 |> Units.Purchased (26.99m |> Euros) (365 |> daysAgo)
                10 |> Units.Purchased (25.99m |> Euros) (100 |> daysAgo)
                11 |> Units.Remaining(50 |> daysAgo) // 2 units lost
            ]
        yield!
            ISBN.JavaScriptTheGoodParts.Events [ // ↩
                8 |> Units.Purchased (15.10m |> Euros) (365 |> daysAgo)
            ]
        yield!
            ISBN.ThePragmaticProgrammer.Events [ // ↩
                8 |> Units.Purchased (27.80m |> Euros) (365 |> daysAgo)
            ]
    ]

[<RequireQualifiedAccess>]
module StockEventRepository =
    let internal instance = StockEventRepository Fakes.oneYear