[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Warehouse

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data

type private Units =
    static member private For eventType date quantity : StockEvent = {
        SKU = SKU.none
        Date = date
        Quantity = quantity
        Type = eventType
    }

    static member Purchased price =
        Units.For(EventType.ProductReceived price)

    static member Remaining = Units.For EventType.StockAdjusted

type SKU with
    member sku.Events stockEvents : StockEvent list = [ for stockEvent in stockEvents -> { stockEvent with SKU = sku } ]

let oneYear =
    ResizeArray [
        yield!
            SKU.CleanArchitecture.Events [ // ↩
                10 |> Units.Purchased 24.99m<euros> (365 |> daysAgo)
            ]
        yield!
            SKU.DomainDrivenDesign.Events [ // ↩
                8 |> Units.Purchased 44.30m<euros> (365 |> daysAgo)
            ]
        yield!
            SKU.DomainModelingMadeFunctional.Events [
                15 |> Units.Purchased 25.99m<euros> (365 |> daysAgo)
                10 |> Units.Purchased 25.99m<euros> (100 |> daysAgo)
                11 |> Units.Remaining(50 |> daysAgo) // 2 units lost
            ]
        yield!
            SKU.JavaScriptTheGoodParts.Events [ // ↩
                8 |> Units.Purchased 15.10m<euros> (365 |> daysAgo)
            ]
        yield!
            SKU.ThePragmaticProgrammer.Events [ // ↩
                8 |> Units.Purchased 27.80m<euros> (365 |> daysAgo)
            ]
    ]