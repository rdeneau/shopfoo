namespace Shopfoo.Product.Tests

open System
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Tests
open Swensen.Unquote
open TUnit.Core

type ReceiveSupplyShould() =
    [<Test>]
    [<Arguments(0)>]
    [<Arguments(-1)>]
    [<Arguments(-10)>]
    member _.``reject supply when quantity is zero or negative`` quantity =
        async {
            let input: ReceiveSupplyInput = {
                SKU = (ISBN "978-0-13-468599-1").AsSKU
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
                Quantity = quantity
                PurchasePrice = Dollars 24.99m
            }

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.ReceiveSupply input

            let expectedError = { EntityName = "Quantity"; ErrorMessage = $"should be positive but was %i{quantity}" }
            result =! Error(Error.Validation [ expectedError ])
        }

    [<Test>]
    [<Arguments(0)>]
    [<Arguments(-19.99)>]
    member _.``reject supply when purchase price is zero or negative``(price: double) =
        async {
            let price = decimal price

            let input: ReceiveSupplyInput = {
                SKU = (ISBN "978-0-13-468599-2").AsSKU
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
                Quantity = 5
                PurchasePrice = Dollars price
            }

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.ReceiveSupply input

            let expectedError = { EntityName = "PurchasePrice"; ErrorMessage = $"should be positive but was %M{price}" }
            result =! Error(Error.Validation [ expectedError ])
        }

    [<Test>]
    member _.``create stock event when input is valid``() =
        async {
            let isbn = ISBN "978-0-13-468599-3"
            let sku = isbn.AsSKU
            let date = DateOnly.FromDateTime(DateTime.UtcNow)

            let input: ReceiveSupplyInput = {
                SKU = sku
                Date = date
                Quantity = 5
                PurchasePrice = Dollars 24.99m
            }

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.ReceiveSupply input
            result =! Ok()

            let! stock = fixture.Api.DetermineStock sku
            stock =! Ok { SKU = sku; Quantity = 5 }
        }