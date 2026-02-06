namespace Shopfoo.Product.Tests

open System
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects.Dependencies
open Shopfoo.Effects.Interpreter.Monitoring
open Shopfoo.Product
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary
open Shopfoo.Product.Data.Prices
open Shopfoo.Product.Data.Sales
open Shopfoo.Product.Data.Warehouse
open Shopfoo.Product.DependencyInjection
open Shopfoo.Product.Tests.Mocks.FakeStoreClientMock
open Shopfoo.Product.Tests.Mocks.OpenLibraryClientMock

type ApiTestFixture(?books: BookRaw list, ?pricesSet: Prices list, ?sales: Sale list, ?stockEvents: StockEvent list) =
    static let nullPipelineLoggerFactory =
        { new IPipelineLoggerFactory with
            member _.CreateLogger(categoryName) =
                { new IPipelineLogger with
                    member _.LogPipeline name pipeline arg = pipeline arg
                }
        }

    static let nullPipelineTimer =
        { new IPipelineTimer with
            member _.TimeCommand name pipeline arg = pipeline arg
            member _.TimeQuery name pipeline arg = pipeline arg
            member _.TimeQueryOptional name pipeline arg = pipeline arg
        }

    // Build the service collection based on production dependencies overriden with test ones
    let services =
        ServiceCollection()
            // Core/Effects
            .AddEffects() // Production dependencies
            .AddSingleton<IPipelineLoggerFactory>(nullPipelineLoggerFactory)
            .AddSingleton<IPipelineTimer>(nullPipelineTimer)
            // Feat/Product
            .AddProductApi() // Production dependencies
            .AddSingleton<IFakeStoreClient, FakeStoreClientMock>()
            .AddSingleton<IOpenLibraryClient, OpenLibraryClientMock>()
            .AddSingleton(BooksRepository.ofList (defaultArg books []))
            .AddSingleton(PricesRepository.ofList (defaultArg pricesSet []))
            .AddSingleton(SalesRepository(defaultArg sales []))
            .AddSingleton(StockEventRepository(defaultArg stockEvents []))

    let serviceProvider = services.BuildServiceProvider()

    member val Api = serviceProvider.GetRequiredService<IProductApi>()

    interface IDisposable with
        member _.Dispose() = serviceProvider.Dispose()