namespace Shopfoo.Product.Tests

open System
open Shopfoo.Product.Tests
open Shopfoo.Product.Tests.Fakes
open TUnit.Core

type AddProductShould() =
    [<Test>]
    member this.``add product and prices without downcasting errors``() =
        async {
            use fixture = new ApiTestFixture()
            let product = createValidBookProduct ()

            let! result = fixture.Api.AddProduct product

            result
            |> function
                | Ok() -> ()
                | Error _ -> raise (Exception("Test failed with error"))
        }