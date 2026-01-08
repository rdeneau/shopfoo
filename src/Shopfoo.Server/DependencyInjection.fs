module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.DependencyInjection
open Shopfoo.Effects.Dependencies
open Shopfoo.Home.DependencyInjection
open Shopfoo.Product.DependencyInjection

type IServiceCollection with
    member services.AddRemotingApi() =
        services.AddEffects() |> ignore

        services
            .AddProductApi() // ↩
            .AddHomeApi()
            .AddSingleton<Remoting.FeatApi>()
        |> ignore

        services
            .AddSingleton<Remoting.Catalog.CatalogApiBuilder>()
            .AddSingleton<Remoting.Home.HomeApiBuilder>()
            .AddSingleton<Remoting.Prices.PricesApiBuilder>()
            .AddSingleton<Remoting.RootApiBuilder>()