[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Warehouse

open System
open System.Linq
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data

module private Fakes =
    type private Units =
        static member private For eventType date quantity : StockEvent = {
            SKU = SKUUnknown
            Date = date
            Quantity = quantity
            Type = eventType
        }

        static member Purchased price =
            Units.For(EventType.ProductSupplyReceived price)

        static member Remaining = Units.For EventType.StockAdjusted

    type SKU with
        member sku.Events stockEvents : StockEvent list = [ for stockEvent in stockEvents -> { stockEvent with SKU = sku } ]

    let oneYear =
        ResizeArray [
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

module Pipeline =
    let private repository =
        Fakes.oneYear
        |> Seq.groupBy _.SKU
        |> Seq.map (fun (sku, sales) -> sku, sales |> ResizeArray)
        |> _.ToDictionary(fst, snd)

    let adjustStock { Stock.SKU = sku; Quantity = quantity } =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency

            let adjustmentEvent = {
                SKU = sku
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
                Quantity = quantity
                Type = EventType.StockAdjusted
            }

            match repository.TryGetValue(sku) with
            | true, events -> events.Add(adjustmentEvent)
            | false, _ -> repository.Add(sku, ResizeArray [ adjustmentEvent ])

            return Ok()
        }

    let getStockEvents sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 150) // Simulate latency

            match repository.TryGetValue(sku) with
            | true, events -> return Some(events |> List.ofSeq)
            | false, _ -> return None
        }