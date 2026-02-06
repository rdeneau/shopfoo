namespace Shopfoo.Product.Tests

open System
open Shopfoo.Domain.Types
open Shopfoo.Product
open Shopfoo.Product.Tests
open TUnit.Core

type DetermineStockWorkflowShould() =
    [<Test>]
    member this.``execute without downcasting errors``() =
        async {
            use fixture = new ApiTestFixture()
            let api = fixture.GetService<IProductApi>()
            let isbn = ISBN "978-0-13-468599-1"
            let sku = isbn.AsSKU

            let! result = api.DetermineStock sku

            result
            |> function
                | Ok _ -> ()
                | Error e -> raise (Exception($"Test failed with error: {e}", Exception("Inner")))
        }