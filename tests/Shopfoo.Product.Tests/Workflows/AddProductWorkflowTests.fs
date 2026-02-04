namespace Shopfoo.Product.Tests.Workflows

open System
open Swensen.Unquote
open TUnit.Core
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Effects
open Shopfoo.Effects.Dependencies
open Shopfoo.Effects.Interpreter
open Shopfoo.Effects.Interpreter.Monitoring
open Shopfoo.Product

/// Canary test to detect downcasting issues with the parallel Program execution
type AddProductWorkflowTests() =

    member _.createMockInterpreterFactory() : IInterpreterFactory =
        { new IInterpreterFactory with
            member _.Create(domain: #IDomain) =
                let loggerFactory =
                    { new IPipelineLoggerFactory with
                        member _.CreateLogger(categoryName) =
                            { new IPipelineLogger with
                                member _.LogPipeline name pipeline arg = pipeline arg
                            }
                    }

                let timer =
                    { new IPipelineTimer with
                        member _.TimeCommand name pipeline arg = pipeline arg
                        member _.TimeQuery name pipeline arg = pipeline arg
                        member _.TimeQueryOptional name pipeline arg = pipeline arg
                    }

                Interpreter(domain, loggerFactory, timer)
        }

    member _.createValidBookProduct() : Product =
        let isbn = ISBN "978-0-13-468599-1"

        {
            SKU = isbn.AsSKU
            Title = "Clean Code"
            Description = "A Handbook of Agile Software Craftsmanship"
            Category =
                Category.Books {
                    ISBN = isbn
                    Subtitle = "A Handbook of Agile Software Craftsmanship"
                    Authors = Set [ { OLID = OLID "OL1234567A"; Name = "Robert C. Martin" } ]
                    Tags = Set []
                }
            ImageUrl = ImageUrl.Valid "https://example.com/clean-code.jpg"
        }

    [<Test>]
    member this.``AddProductWorkflow should add product and prices without downcasting errors``() =
        async {
            let interpreterFactory = this.createMockInterpreterFactory ()
            let api: IProductApi = Api(interpreterFactory, null, null)
            let product = this.createValidBookProduct ()

            let! result = api.AddProduct product

            result
            |> function
                | Ok() -> ()
                | Error _ -> raise (Exception("Test failed with error"))
        }