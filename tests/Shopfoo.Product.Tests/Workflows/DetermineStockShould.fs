namespace Shopfoo.Product.Tests

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data.Helpers
open Shopfoo.Product.Tests
open Swensen.Unquote
open TUnit.Core

type DetermineStockShould() =
    [<Test>]
    member _.``deduct sales from initial stock``() =
        async {
            let isbn = ISBN "1"
            let sku = isbn.AsSKU

            use fixture =
                new ApiTestFixture(
                    stockEvents =
                        isbn.Events [ // ↩
                            10 |> Units.Purchased (Dollars 24.99m) (365 |> daysAgo)
                        ],
                    sales =
                        isbn.Sales [
                            1 |> unitsSold (Dollars 38.99m) (342 |> daysAgo)
                            2 |> unitsSold (Dollars 38.99m) (231 |> daysAgo)
                            1 |> unitsSold (Dollars 39.90m) (120 |> daysAgo)
                        ]
                )

            let! result = fixture.Api.DetermineStock sku
            result =! Ok { Stock.SKU = sku; Quantity = 6 } // 10 - 1 - 2 - 1 = 6
        }

    [<Test>]
    member _.``take into account stock arrivals after sales``() =
        async {
            let isbn = ISBN "2"
            let sku = isbn.AsSKU

            use fixture =
                new ApiTestFixture(
                    stockEvents =
                        isbn.Events [ // ↩
                            10 |> Units.Purchased (Euros 24.99m) (350 |> daysAgo)
                            10 |> Units.Purchased (Euros 25.15m) (300 |> daysAgo)
                        ],
                    sales =
                        isbn.Sales [ // ↩
                            8 |> unitsSold (Euros 38.99m) (310 |> daysAgo)
                        ]
                )

            let! result = fixture.Api.DetermineStock sku
            result =! Ok { Stock.SKU = sku; Quantity = 12 } // 10 - 8 + 10 = 12
        }

    [<Test>]
    member _.``take into account stock adjustment, ignoring previous events``() =
        async {
            let isbn = ISBN "3"
            let sku = isbn.AsSKU

            use fixture =
                new ApiTestFixture(
                    stockEvents =
                        isbn.Events [
                            15 |> Units.Purchased (25.99m |> Euros) (365 |> daysAgo)
                            10 |> Units.Purchased (25.99m |> Euros) (100 |> daysAgo)
                            11 |> Units.Remaining(50 |> daysAgo) // 2 units lost
                        ],
                    sales =
                        isbn.Sales [
                            4 |> unitsSold (43.04m |> Euros) (336 |> daysAgo)
                            3 |> unitsSold (43.04m |> Euros) (225 |> daysAgo)
                            5 |> unitsSold (32.32m |> Euros) (114 |> daysAgo)
                            8 |> unitsSold (32.32m |> Euros) (045 |> daysAgo)
                        ]
                )

            let! result = fixture.Api.DetermineStock sku
            result =! Ok { Stock.SKU = sku; Quantity = 3 } // 11 - 8 = 3
        }

    [<Test>]
    member _.``differentiate stock by SKU``() =
        async {
            let isbn1, isbn2 = ISBN "1", ISBN "2"
            let sku1, sku2 = isbn1.AsSKU, isbn2.AsSKU

            use fixture =
                new ApiTestFixture(
                    stockEvents = [
                        yield!
                            isbn1.Events [ // ↩
                                10 |> Units.Purchased (Dollars 24.99m) (365 |> daysAgo)
                            ]
                        yield!
                            isbn2.Events [ // ↩
                                8 |> Units.Purchased (27.80m |> Euros) (365 |> daysAgo)
                            ]
                    ],
                    sales = [
                        yield!
                            isbn1.Sales [ // ↩
                                2 |> unitsSold (Dollars 38.99m) (231 |> daysAgo)
                                1 |> unitsSold (Dollars 39.90m) (120 |> daysAgo)
                            ]
                        yield!
                            isbn2.Sales [ // ↩
                                1 |> unitsSold (49.37m |> Euros) (364 |> daysAgo)
                                3 |> unitsSold (32.86m |> Euros) (119 |> daysAgo)
                            ]
                    ]
                )

            let! result1 = fixture.Api.DetermineStock sku1
            let! result2 = fixture.Api.DetermineStock sku2

            [ result1; result2 ]
            =! [
                Ok { Stock.SKU = sku1; Quantity = 7 } // 10 - 2 - 1 = 7
                Ok { Stock.SKU = sku2; Quantity = 4 } // 8 - 1 - 3 = 4
            ]
        }