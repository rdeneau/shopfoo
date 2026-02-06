module Shopfoo.Product.DependencyInjection

open System
open System.Net.Http
open Shopfoo.Data.DependencyInjection
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Data
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options

module Sections =
    [<Literal>]
    let rec FakeStore = nameof FakeStore

    [<Literal>]
    let rec OpenLibrary = nameof OpenLibrary

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
            .AddClient<FakeStoreClient, FakeStoreSettings>(_.BaseUrl)
            .AddClient<OpenLibraryClient, OpenLibrarySettings>(_.BaseUrl)
            .AddSingleton<OpenLibraryClientSettings>(fun sp ->
                let options = sp.GetRequiredService<IOptions<OpenLibrarySettings>>()
                { CoverBaseUrl = options.Value.CoverBaseUrl }
            )
            .AddSingleton<IFakeStoreClient>(fun sp -> sp.GetRequiredService<FakeStoreClient>() :> IFakeStoreClient)
            .AddSingleton<IOpenLibraryClient>(fun sp -> sp.GetRequiredService<OpenLibraryClient>() :> IOpenLibraryClient)
            .AddSingleton<Sales.SaleRepository>(Sales.Fakes.repository)
            .AddSingleton<Warehouse.StockEventRepository>(Warehouse.Fakes.repository)
            .AddSingleton<IProductApi, Api>()