[<RequireQualifiedAccess>]
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

module internal Pipeline =
    let adjustStock (repository: StockEventRepository) { Stock.SKU = sku; Quantity = quantity } =
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

    let getStockEvents (repository: StockEventRepository) sku =
        async {
            do! Fake.latencyInMilliseconds 150
            return repository.GetStockEvents sku
        }

[<RequireQualifiedAccess>]
module internal Fakes =
    let private oneYear = [
        yield!
            ISBN.CleanArchitecture.Events [ // ↩
                10 |> Units.Purchased (Dollars 24.99m) (365 |> daysAgo)
            ]
        yield!
            ISBN.DomainDrivenDesign.Events [ // ↩
                8 |> Units.Purchased (44.30m |> Euros) (365 |> daysAgo)
            ]
        yield!
            ISBN.DomainModelingMadeFunctional.Events [
                15 |> Units.Purchased (25.99m |> Euros) (365 |> daysAgo)
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

    let repository = StockEventRepository oneYear