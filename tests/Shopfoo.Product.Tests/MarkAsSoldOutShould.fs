namespace Shopfoo.Product.Tests

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data.Helpers
open Shopfoo.Product.Tests.Types
open Swensen.Unquote
open TUnit.Core

type MarkAsSoldOutShould() =
    [<Test>]
    [<Arguments(1)>]
    [<Arguments(5)>]
    [<Arguments(100)>]
    member _.``be rejected given a stock quantity greater than zero``(n: int) =
        async {
            let isbn = ISBN "978-0-13-468599-1"
            use fixture = new ApiTestFixture(stockEvents = isbn.Events [ n |> Units.Purchased (Euros 24.99m) (365 |> daysAgo) ])
            let expectedError = { EntityName = "Stock"; ErrorMessage = "Stock quantity must be zero to mark as sold out." }

            let! result = fixture.Api.MarkAsSoldOut isbn.AsSKU
            result =! Error(Error.Validation [ expectedError ])
        }

    [<Test>]
    [<Arguments(CurrencyEnum.EUR, 19.99)>]
    [<Arguments(CurrencyEnum.USD, 24.99)>]
    member _.``update retail price to SoldOut given a product with no stock`` (Currency.FromEnum currency) retailPrice =
        async {
            let isbn = ISBN "978-0-13-468599-2"
            let sku = isbn.AsSKU
            let prices = Prices.Create(isbn, currency, retailPrice)
            use fixture = new ApiTestFixture(pricesSet = [ prices ])

            let! result = fixture.Api.MarkAsSoldOut sku
            result =! Ok()

            let! actualPrices = fixture.Api.GetPrices sku
            actualPrices =! Some { prices with RetailPrice = RetailPrice.SoldOut }
        }