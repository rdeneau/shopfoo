namespace Shopfoo.Product.Tests.Workflows

open System
open Shopfoo.Product
open Shopfoo.Product.Tests.Fakes
open TUnit.Core

type AddProductWorkflowShould() =
    [<Test>]
    member this.``add product and prices without downcasting errors``() =
        async {
            let interpreterFactory = MockInterpreterFactory()
            let api: IProductApi = Api(interpreterFactory, null, null)
            let product = createValidBookProduct ()

            let! result = api.AddProduct product

            result
            |> function
                | Ok() -> ()
                | Error _ -> raise (Exception("Test failed with error"))
        }
