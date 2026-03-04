namespace Shopfoo.Product.Tests.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data
open Shopfoo.Product.Tests.Workflows.Examples
open Shopfoo.Tests.Common
open TUnit.Core

type ResetAllCachesShould() =
    [<Test>]
    member _.``restore seeded book after modifications``() =
        async {
            use fixture = new ApiTestFixture()

            // Arrange
            let! _ = fixture.Api.ResetAllCaches() // Needed to seed it with Fakes.allBooks, as ApiTestFixture starts with an empty BooksRepository
            let book = Books.Fakes.allBooks |> List.randomChoice
            let sku = book.ISBN.AsSKU
            let! product = fixture.Api.GetProduct sku
            assumeThat "Picked book is part of the seed data" <@ product |> Option.map _.SKU = Some sku @>

            let original = product |> Option.get
            let! saveResult = fixture.Api.SaveProduct { original with Title = "Modified" }
            let! modified = fixture.Api.GetProduct sku
            assumeThat "Product has been modified" <@ saveResult = Ok() && modified |> Option.map _.Title = Some "Modified" @>

            // Act
            let! resetResult = fixture.Api.ResetAllCaches()

            // Assert
            let! afterReset = fixture.Api.GetProduct sku
            testThat "Product returned to its original state" <@ resetResult = Ok() && afterReset = Some original @>
        }

    [<Test>]
    member _.``clear non-seed data after reset``() =
        async {
            use fixture = new ApiTestFixture()

            // Arrange
            let! _ = fixture.Api.ResetAllCaches()
            let product = CleanCode.Domain.product // Alternate version of the seed book, with an ISBN formatted differently
            let sku = product.SKU
            let! addResult = fixture.Api.AddProduct(product, Currency.EUR)
            let! productBefore = fixture.Api.GetProduct sku
            let! pricesBefore = fixture.Api.GetPrices sku

            assumeThat
                "Non-seed product and its prices have been added"
                <@
                    addResult = Ok()
                    && productBefore |> Option.map _.SKU = Some sku
                    && pricesBefore |> Option.map _.SKU = Some sku
                @>

            // Act
            let! resetResult = fixture.Api.ResetAllCaches()

            // Assert
            let! productAfter = fixture.Api.GetProduct sku
            let! pricesAfter = fixture.Api.GetPrices sku
            testThat "Non-seed product and prices are cleared" <@ resetResult = Ok() && productAfter = None && pricesAfter = None @>
        }