module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module UI =
    open Shopfoo.Catalog.DependencyInjection

    type IServiceCollection with
        member services.AddRemotingApi() =
            services
                .AddCatalogApi()
                .AddSingleton<Feat.Home.Api>()
                .AddSingleton<Remoting.FeatApi>()
                .AddSingleton<Remoting.Home.HomeApiBuilder>()
                .AddSingleton<Remoting.Product.ProductApiBuilder>()
                .AddSingleton<Remoting.RootApiBuilder>()