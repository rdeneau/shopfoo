module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Shopfoo.Effects.Dependencies
open Shopfoo.Home.DependencyInjection
open Shopfoo.Product.DependencyInjection

type IServiceCollection with
    member services.AddRemotingApi(configuration: IConfiguration) =
        services.AddEffects() |> ignore

        services
            .Configure<OpenLibrary.Settings>(configuration.GetSection(OpenLibrary.SectionName))
            .AddProductApi()
            .AddHomeApi()
            .AddSingleton<Remoting.FeatApi>()
        |> ignore

        services
            .AddSingleton<Remoting.Catalog.CatalogApiBuilder>()
            .AddSingleton<Remoting.Home.HomeApiBuilder>()
            .AddSingleton<Remoting.Prices.PricesApiBuilder>()
            .AddSingleton<Remoting.RootApiBuilder>()