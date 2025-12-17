module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module UI =
    open Shopfoo.Catalog.DependencyInjection
    open Shopfoo.Home.DependencyInjection

    type IServiceCollection with
        member services.AddRemotingApi() =
            services
                .AddCatalogApi() // ↩
                .AddHomeApi()
                .AddSingleton<Remoting.FeatApi>()
            |> ignore

            services
                .AddSingleton<Remoting.Home.HomeApiBuilder>()
                .AddSingleton<Remoting.Product.ProductApiBuilder>()
                .AddSingleton<Remoting.RootApiBuilder>()