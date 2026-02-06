namespace Shopfoo.Product.Tests.Workflows

open System
open Shopfoo.Domain.Types
open Shopfoo.Product
open TUnit.Core

type DetermineStockWorkflowShould() =
    [<Test>]
    member this.``execute without downcasting errors``() =
        async {
            let interpreterFactory = MockInterpreterFactory()
            let api: IProductApi = Api(interpreterFactory, null, null)
            let isbn = ISBN "978-0-13-468599-1"
            let sku = isbn.AsSKU

            let! result = api.DetermineStock sku

            result
            |> function
                | Ok _ -> ()
                | Error e -> raise (Exception($"Test failed with error: {e}", Exception("Inner")))
        }
