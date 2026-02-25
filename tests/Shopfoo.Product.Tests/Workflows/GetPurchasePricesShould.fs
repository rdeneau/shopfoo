namespace Shopfoo.Product.Tests

open FsCheck
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data.Helpers
open Shopfoo.Product.Tests
open Shopfoo.Product.Tests.Types.Purchases
open Shopfoo.Tests.Common.FsCheckArbs
open Swensen.Unquote
open TUnit.Core

type GetPurchasePricesShould() =
    [<Test; ShopfooFsCheckProperty(MaxTest = 10)>]
    member _.``return no prices given no stock events``(isbn: ISBN) =
        async {
            use fixture = new ApiTestFixture()
            let! result = fixture.Api.GetPurchasePrices isbn.AsSKU
            result =! PurchasePrices.Empty
        }

    [<Test; ShopfooFsCheckProperty(MaxTest = 30)>]
    member _.``return no average prices given all purchases are older than one year``(isbn: ISBN, ManyPurchasesInEuros purchases) =
        async {
            let events = purchases |> List.mapi (fun i x -> x.ToStockEvent(DaysAgo.Before1Y i))
            use fixture = new ApiTestFixture(stockEvents = isbn.Events events)
            let! result = fixture.Api.GetPurchasePrices isbn.AsSKU
            test <@ result.AverageOver1Y.IsNone @>
        }

    [<Test; ShopfooFsCheckProperty(MaxTest = 30)>]
    member _.``return no average prices given purchases in 1Y with mixed currencies``
        (isbn: ISBN, ManyPurchasesInEuros eurPurchases, ManyPurchasesInDollars usdPurchases)
        =
        async {
            use fixture =
                new ApiTestFixture(
                    stockEvents =
                        isbn.Events [
                            yield! eurPurchases |> List.mapi (fun i x -> x.ToStockEvent(DaysAgo.Within1Y i))
                            yield! usdPurchases |> List.mapi (fun i x -> x.ToStockEvent(DaysAgo.Within1Y i))
                        ]
                )

            let! result = fixture.Api.GetPurchasePrices isbn.AsSKU
            result.AverageOver1Y =! None
        }

    [<Test; ShopfooFsCheckProperty(MaxTest = 30)>]
    member _.``return last price from most recent purchase event``(isbn: ISBN, ManyPurchasesInEuros purchases) =
        async {
            let events = purchases |> List.mapi (fun i x -> x.ToStockEvent(DaysAgo.Within1Y i))
            use fixture = new ApiTestFixture(stockEvents = isbn.Events events)
            let! result = fixture.Api.GetPurchasePrices isbn.AsSKU
            test <@ (result.LastPrice |> Option.map fst) = (purchases |> List.map _.Price |> List.tryLast) @>
        }

    [<Test; ShopfooFsCheckProperty(MaxTest = 15)>] // MaxTest is low because this test is too slow
    member _.``return the average price of purchases within 1Y``
        (isbn: ISBN, ManyPurchasesInEuros purchases, PositiveInt factor, RandomFromSeed random)
        =
        let getAverageOver1Y events =
            async {
                use fixture = new ApiTestFixture(stockEvents = isbn.Events events)
                let! res = fixture.Api.GetPurchasePrices isbn.AsSKU
                return res.AverageOver1Y
            }

        async {
            let baseEvents = purchases |> List.mapi (fun i x -> x.ToStockEvent(DaysAgo.Within1Y i))
            let! baseAverage = getAverageOver1Y baseEvents

            // Properties of the weighted average calculation:
            // - Bounded: The average price should always be between the minimum and maximum purchase prices.
            let prices = purchases |> List.map _.Price
            let minPrice = prices |> List.min
            let maxPrice = prices |> List.max

            // - Zero: If all stock arrivals have a quantity of 0, the average should be None (since it would be undefined).
            let zeroQtyEvents = baseEvents |> List.map (fun e -> { e with Quantity = 0 })
            let! zeroQtyAverage = getAverageOver1Y zeroQtyEvents
            zeroQtyAverage =! None

            // - Homogeneity: If you multiply the quantity by a constant factor but keep the prices the same, the average price remains identical.
            let scaledEvents = baseEvents |> List.map (fun e -> { e with Quantity = e.Quantity * factor })
            let! scaledAverage = getAverageOver1Y scaledEvents

            // - Idempotency: If all stock arrivals have the exact same price P, the average must be exactly P.
            let randomPrice = purchases |> List.map _.Price |> List.randomChoiceWith random
            let uniformPriceEvents = baseEvents |> List.map (StockEvent.mapPurchasePrice (fun _ -> randomPrice))
            let! uniformAverage = getAverageOver1Y uniformPriceEvents

            test
                <@
                    baseAverage >= Some minPrice
                    && baseAverage <= Some maxPrice
                    && scaledAverage = baseAverage
                    && zeroQtyAverage.IsNone
                    && uniformAverage = Some randomPrice
                @>
        }