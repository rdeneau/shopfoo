namespace Shopfoo.Product.Tests

open System
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
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
open Shopfoo.Program.Dependencies
open Shopfoo.Program.Monitoring

type ApiTestFixture(?books: BookRaw list, ?pricesSet: Prices list, ?sales: Sale list, ?stockEvents: StockEvent list) =
    static let nullWorkMonitor = WorkMonitor(fun _ work -> work)

    static let nullWorkLogger =
        { new IWorkLogger with
            member _.Logger() = nullWorkMonitor
        }

    static let nullWorkMonitors =
        { new IWorkMonitors with
            member _.LoggerFactory _ = nullWorkLogger
            member _.CommandTimer() = nullWorkMonitor
            member _.QueryTimer() = nullWorkMonitor
        }

    // Build the service collection based on production dependencies overriden with test ones
    let services =
        ServiceCollection()
            // Core/Effects
            .AddEffects() // Production dependencies
            .AddSingleton<IWorkMonitors>(nullWorkMonitors)
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