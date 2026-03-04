namespace Shopfoo.Server.Remoting

open Shopfoo.Shared.Remoting

type RootApiBuilder
    (
        adminApiBuilder: Admin.AdminApiBuilder,
        catalogApiBuilder: Catalog.CatalogApiBuilder, // ↩
        homeApiBuilder: Home.HomeApiBuilder,
        pricesApiBuilder: Prices.PricesApiBuilder
    ) =
    member _.Build() : RootApi = {
        Admin = adminApiBuilder.Build()
        Catalog = catalogApiBuilder.Build()
        Home = homeApiBuilder.Build()
        Prices = pricesApiBuilder.Build()
    }