namespace Shopfoo.Product.Tests

open System
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects.Dependencies
open Shopfoo.Effects.Interpreter.Monitoring
open Shopfoo.Product
open Shopfoo.Product.Data
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary
open Shopfoo.Product.DependencyInjection
open Shopfoo.Product.Tests.Mocks.FakeStoreClientMock
open Shopfoo.Product.Tests.Mocks.OpenLibraryClientMock

type ApiTestFixture(?sales: Sale list, ?stockEvents: StockEvent list) =
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

    let addSaleRepository (services: IServiceCollection) =
        match sales with
        | None
        | Some [] -> services
        | Some sales -> services.AddSingleton(Sales.SaleRepository sales)

    let addStockEventRepository (services: IServiceCollection) =
        match stockEvents with
        | None
        | Some [] -> services
        | Some stockEvents -> services.AddSingleton(Warehouse.StockEventRepository stockEvents)

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
        |> addSaleRepository
        |> addStockEventRepository

    let serviceProvider = services.BuildServiceProvider()

    member val Api = serviceProvider.GetRequiredService<IProductApi>()

    interface IDisposable with
        member _.Dispose() = serviceProvider.Dispose()