module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module UI =
    type IServiceCollection with
        member services.AddRemotingApi() =
            services // ↩
                .AddSingleton<Remoting.CatalogFeat.Api>()
                .AddSingleton<Remoting.HomeFeat.Api>()
                .AddSingleton<Remoting.FeatApi>()
                .AddSingleton<Remoting.Home.HomeApiBuilder>()
                .AddSingleton<Remoting.Product.ProductApiBuilder>()
                .AddSingleton<Remoting.RootApiBuilder>()