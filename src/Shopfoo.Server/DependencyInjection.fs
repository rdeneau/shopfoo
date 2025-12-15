module Shopfoo.Server.DependencyInjection

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Abstractions

[<AutoOpen>]
module UI =
    type IServiceCollection with
        member services.AddRemotingApi() =
            services
                .AddSingleton<ILogger>(NullLogger.Instance) // TODO: use NLog?
                .AddSingleton<Feat.Catalog.Api>()
                .AddSingleton<Feat.Home.Api>()
                .AddSingleton<Remoting.FeatApi>()
                .AddSingleton<Remoting.Home.HomeApiBuilder>()
                .AddSingleton<Remoting.Product.ProductApiBuilder>()
                .AddSingleton<Remoting.RootApiBuilder>()