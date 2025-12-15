module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module UI =
    type IServiceCollection with
        member services.AddRemotingApi() =
            services // ↩
                .AddSingleton<Feat.Catalog.Api>()
                .AddSingleton<Feat.Home.Api>()
                .AddSingleton<Remoting.FeatApi>()
                .AddSingleton<Remoting.Home.HomeApiBuilder>()
                .AddSingleton<Remoting.Product.ProductApiBuilder>()
                .AddSingleton<Remoting.RootApiBuilder>()