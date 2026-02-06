namespace Shopfoo.Product.Tests

open System
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Effects.Dependencies
open Shopfoo.Product
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary
open Shopfoo.Product.Tests.Mocks.FakeStoreClientMock
open Shopfoo.Product.Tests.Mocks.OpenLibraryClientMock

type ApiTestFixture() =
    let createMockProductApi (sp: IServiceProvider) : IProductApi =
        let interpreterFactory = Shopfoo.Product.Tests.Workflows.MockInterpreterFactory()
        let fakeStoreClient = sp.GetRequiredService<IFakeStoreClient>()
        let openLibraryClient = sp.GetRequiredService<IOpenLibraryClient>()
        Api(interpreterFactory, fakeStoreClient, openLibraryClient)

    let addMockProductApi (services: IServiceCollection) =
        services
            .AddSingleton<IFakeStoreClient, FakeStoreClientMock>()
            .AddSingleton<IOpenLibraryClient, OpenLibraryClientMock>()
            .AddSingleton<IProductApi>(Func<IServiceProvider, IProductApi>(createMockProductApi))

    let serviceProvider =
        let services = ServiceCollection()
        (services |> addMockProductApi).AddEffects().BuildServiceProvider()

    member _.GetService<'T>() : 'T = serviceProvider.GetRequiredService<'T>()

    interface IDisposable with
        member _.Dispose() = serviceProvider.Dispose()