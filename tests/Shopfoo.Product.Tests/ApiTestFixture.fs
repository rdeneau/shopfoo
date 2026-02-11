namespace Shopfoo.Product.Tests

open System
open Microsoft.Extensions.DependencyInjection
open NSubstitute
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
open Shopfoo.Program.Tests.Mocks

type ApiTestFixture(?books: BookRaw list, ?pricesSet: Prices list, ?sales: Sale list, ?stockEvents: StockEvent list) =
    let fakeStoreClientMock = Substitute.For<IFakeStoreClient>()
    let openLibraryClientMock = Substitute.For<IOpenLibraryClient>()

    // Build the service collection based on production dependencies overriden with test ones
    let services =
        ServiceCollection()
            // Core/Program
            .AddProgramMocks()
            // Feat/Product
            .AddProductApi() // Production dependencies
            .AddSingleton<IFakeStoreClient>(fakeStoreClientMock)
            .AddSingleton<IOpenLibraryClient>(openLibraryClientMock)
            .AddSingleton(BooksRepository.ofList (defaultArg books []))
            .AddSingleton(PricesRepository.ofList (defaultArg pricesSet []))
            .AddSingleton(SalesRepository(defaultArg sales []))
            .AddSingleton(StockEventRepository(defaultArg stockEvents []))

    let serviceProvider = services.BuildServiceProvider()

    member val Api = serviceProvider.GetRequiredService<IProductApi>()

    interface IDisposable with
        member _.Dispose() = serviceProvider.Dispose()