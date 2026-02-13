namespace Shopfoo.Product.Tests

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Tests.Types
open Swensen.Unquote
open TUnit.Core

type SavePricesShould() =
    [<Test>]
    [<Arguments(0, CurrencyEnum.EUR, "was 0")>]
    [<Arguments(0, CurrencyEnum.USD, "was 0")>]
    [<Arguments(-19.99, CurrencyEnum.EUR, "was -19.99")>]
    [<Arguments(-24.95, CurrencyEnum.USD, "was -24.95")>]
    member _.``reject invalid RetailPrice`` retailPrice (Currency.FromEnum currency) reason =
        async {
            let prices = {
                SKU = (ISBN "978-0-13-468599-1").AsSKU
                Currency = currency
                RetailPrice = RetailPrice.Regular(Money.ByCurrency currency retailPrice)
                ListPrice = None
            }

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.SavePrices prices

            let expectedError = { EntityName = "RetailPrice"; ErrorMessage = $"should be positive but %s{reason}" }
            result =! Error(Error.Validation [ expectedError ])
        }

    [<Test>]
    [<Arguments(0, CurrencyEnum.EUR, "was 0")>]
    [<Arguments(0, CurrencyEnum.USD, "was 0")>]
    [<Arguments(-19.99, CurrencyEnum.EUR, "was -19.99")>]
    [<Arguments(-24.95, CurrencyEnum.USD, "was -24.95")>]
    member _.``reject invalid ListPrice`` listPrice (Currency.FromEnum currency) reason =
        async {
            let prices = {
                SKU = (ISBN "978-0-13-468599-3").AsSKU
                Currency = currency
                RetailPrice = RetailPrice.Regular(Money.ByCurrency currency (1m + listPrice * listPrice))
                ListPrice = Some(Money.ByCurrency currency listPrice)
            }

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.SavePrices prices

            let expectedError = { EntityName = "ListPrice"; ErrorMessage = $"should be positive but %s{reason}" }
            result =! Error(Error.Validation [ expectedError ])
        }

    member private _.accept(prices: Prices) =
        async {
            let initialPrices = Prices.Initial(prices.SKU, prices.Currency)
            use fixture = new ApiTestFixture(pricesSet = [ initialPrices ])

            let! result = fixture.Api.SavePrices prices
            result =! Ok()

            let! savedPrices = fixture.Api.GetPrices prices.SKU
            savedPrices =! Some prices
        }

    [<Test>]
    [<Arguments(CurrencyEnum.EUR)>]
    [<Arguments(CurrencyEnum.USD)>]
    member this.``accept RetailPrice with ListPrice``(Currency.FromEnum currency) =
        this.accept (Prices.Create((ISBN "978-0-13-468599-5").AsSKU, currency, 19.99m, listPrice = 24.99m))

    [<Test>]
    [<Arguments(CurrencyEnum.EUR)>]
    [<Arguments(CurrencyEnum.USD)>]
    member this.``accept RetailPrice without ListPrice``(Currency.FromEnum currency) =
        this.accept (Prices.Create((ISBN "978-0-13-468599-6").AsSKU, currency, 19.99m))

    [<Test>]
    [<Arguments(CurrencyEnum.EUR)>]
    [<Arguments(CurrencyEnum.USD)>]
    member this.``accept SoldOut with ListPrice``(Currency.FromEnum currency) =
        this.accept {
            SKU = (ISBN "978-0-13-468599-7").AsSKU
            Currency = currency
            RetailPrice = RetailPrice.SoldOut
            ListPrice = Some(Money.ByCurrency currency 24.99m)
        }

    [<Test>]
    [<Arguments(CurrencyEnum.EUR)>]
    [<Arguments(CurrencyEnum.USD)>]
    member this.``accept SoldOut without ListPrice``(Currency.FromEnum currency) =
        this.accept {
            SKU = (ISBN "978-0-13-468599-8").AsSKU
            Currency = currency
            RetailPrice = RetailPrice.SoldOut
            ListPrice = None
        }