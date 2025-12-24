namespace Shopfoo.Server.Remoting

open Shopfoo.Shared.Remoting

type RootApiBuilder
    (
        catalogApiBuilder: Catalog.CatalogApiBuilder, // ↩
        homeApiBuilder: Home.HomeApiBuilder,
        pricesApiBuilder: Prices.PricesApiBuilder
    ) =
    member _.Build() : RootApi = {
        Catalog = catalogApiBuilder.Build()
        Home = homeApiBuilder.Build()
        Prices = pricesApiBuilder.Build()
    }