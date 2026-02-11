module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Data.DependencyInjection
open Shopfoo.Home.DependencyInjection
open Shopfoo.Product.DependencyInjection
open Shopfoo.Program.Dependencies

type IServiceCollection with
    member services.AddRemotingApi(configuration: IConfiguration) =
        services.AddProgram() |> ignore

        services
            .Configure<FakeStoreSettings>(configuration.GetSection(Sections.FakeStore))
            .Configure<OpenLibrarySettings>(configuration.GetSection(Sections.OpenLibrary))
            .AddHttp()
            .AddProductApi()
            .AddHomeApi()
            .AddSingleton<Remoting.FeatApi>()
        |> ignore

        services
            .AddSingleton<Remoting.Catalog.CatalogApiBuilder>()
            .AddSingleton<Remoting.Home.HomeApiBuilder>()
            .AddSingleton<Remoting.Prices.PricesApiBuilder>()
            .AddSingleton<Remoting.RootApiBuilder>()