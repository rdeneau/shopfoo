namespace Shopfoo.Product.Tests.Workflows

open System
open Shopfoo.Domain.Types
open Shopfoo.Product
open TUnit.Core

type DetermineStockWorkflowTests() =
    [<Test>]
    member this.``DetermineStockWorkflow should execute without downcasting errors``() =
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

// NOTE: This test currently fails due to an architectural limitation:
// The interpreter expects all programs to have effects that return Program<Result<'res, Error>>.
// DetermineStockWorkflow binds queries (GetSales, GetStockEvents) that return Program<Option<_>>,
// creating intermediate programs with incompatible effect types.
// Solution: All intermediate programs created via bind operations need to be "promoted" to the
// final result type, or the interpreter needs redesign to support heterogeneous effect types.