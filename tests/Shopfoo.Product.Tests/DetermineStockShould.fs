namespace Shopfoo.Product.Tests

open System
open Shopfoo.Domain.Types
open Shopfoo.Product.Tests
open TUnit.Core

type DetermineStockShould() =
    [<Test>]
    member this.``execute without downcasting errors``() =
        async {
            use fixture = new ApiTestFixture()
            let isbn = ISBN "978-0-13-468599-1"
            let sku = isbn.AsSKU

            let! result = fixture.Api.DetermineStock sku

            result
            |> function
                | Ok _ -> ()
                | Error e -> raise (Exception($"Test failed with error: {e}", Exception("Inner")))
        }