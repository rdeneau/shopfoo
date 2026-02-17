namespace Shopfoo.Product.Tests

open System
open Microsoft.Extensions.DependencyInjection
open NSubstitute
open Shopfoo.Domain.Types.Errors
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

    let returnsAsync value source = source.Returns(task { return value }) |> ignore

    member val Api = serviceProvider.GetRequiredService<IProductApi>()

    member _.ConfigureFakeStoreClient(fakeStoreProducts: ProductDto list) =
        fakeStoreClientMock.GetProductsAsync() |> returnsAsync (Ok fakeStoreProducts)

    member _.ConfigureOpenLibraryClient(?authors: AuthorDto list, ?books: BookDto list, ?works: WorkDto list) =
        // General cases: not found
        let errorNotFound = HttpApiError(HttpApiName.OpenLibrary, HttpStatus.FromHttpStatusCode System.Net.HttpStatusCode.NotFound)
        openLibraryClientMock.GetAuthorAsync(Arg.Any<AuthorKey>()) |> returnsAsync (Error errorNotFound)
        openLibraryClientMock.GetBookAsync(Arg.Any<BookKey>()) |> returnsAsync (Error errorNotFound)
        openLibraryClientMock.GetWorkAsync(Arg.Any<WorkKey>()) |> returnsAsync (Error errorNotFound)

        // Specific cases: found with the given data
        for author in defaultArg authors [] do
            openLibraryClientMock.GetAuthorAsync(AuthorKey.Make author.Key) |> returnsAsync (Ok author)

        for book in defaultArg books [] do
            openLibraryClientMock.GetBookAsync(BookKey.Make book.Key) |> returnsAsync (Ok book)

        for work in defaultArg works [] do
            openLibraryClientMock.GetWorkAsync(WorkKey.Make work.Key) |> returnsAsync (Ok work)

    interface IDisposable with
        member _.Dispose() = serviceProvider.Dispose()