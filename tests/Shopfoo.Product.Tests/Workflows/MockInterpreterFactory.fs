namespace Shopfoo.Product.Tests.Workflows

open Shopfoo.Effects
open Shopfoo.Effects.Dependencies
open Shopfoo.Effects.Interpreter
open Shopfoo.Effects.Interpreter.Monitoring

type MockInterpreterFactory() =
    interface IInterpreterFactory with
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