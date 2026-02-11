module Shopfoo.Product.DependencyInjection

open System
open System.Net.Http
open Shopfoo.Data.DependencyInjection
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Data.Catalog
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Shopfoo.Product.Data.Prices
open Shopfoo.Product.Data.Sales
open Shopfoo.Product.Data.Warehouse

module Sections =
    [<Literal>]
    let FakeStore = "FakeStore"

    [<Literal>]
    let OpenLibrary = "OpenLibrary"

[<CLIMutable>]
type FakeStoreSettings = { BaseUrl: string }

[<CLIMutable>]
type OpenLibrarySettings = { BaseUrl: string; CoverBaseUrl: string }

type IServiceCollection with
    member private services.AddClient<'client, 'settings when 'client: not struct and 'settings: not struct> getBaseUrl =
        services.AddHttpClient<'client>(fun (sp: IServiceProvider) (httpClient: HttpClient) ->
            let opt = sp.GetRequiredService<IOptions<'settings>>()
            httpClient.BaseAddress <- Uri(getBaseUrl opt.Value)
            httpClient.AcceptJson()
        )
        |> ignore

        services

    member services.AddProductApi() =
        services
            // FakeStore
            .AddClient<FakeStoreClient, FakeStoreSettings>(_.BaseUrl)
            .AddSingleton<IFakeStoreClient>(fun sp -> sp.GetRequiredService<FakeStoreClient>() :> IFakeStoreClient)
            .AddSingleton<FakeStorePipeline>()

            // OpenLibrary
            .AddClient<OpenLibraryClient, OpenLibrarySettings>(_.BaseUrl)
            .AddSingleton<OpenLibraryClientSettings>(fun sp ->
                let options = sp.GetRequiredService<IOptions<OpenLibrarySettings>>()
                { CoverBaseUrl = options.Value.CoverBaseUrl }
            )
            .AddSingleton<IOpenLibraryClient>(fun sp -> sp.GetRequiredService<OpenLibraryClient>() :> IOpenLibraryClient)
            .AddSingleton<OpenLibraryPipeline>()

            // Books
            .AddSingleton(BooksRepository.instance)
            .AddSingleton<BooksPipeline>()

            // Catalog (Facade over FakeStore, OpenLibrary, Books)
            .AddSingleton<CatalogPipeline>()

            // Prices
            .AddSingleton(PricesRepository.instance)
            .AddSingleton<PricesPipeline>()

            // Sales
            .AddSingleton(SalesRepository.instance)
            .AddSingleton<SalesPipeline>()

            // Warehouse
            .AddSingleton(StockEventRepository.instance)
            .AddSingleton<WarehousePipeline>()

            // API
            .AddSingleton<IProductApi, Api>()