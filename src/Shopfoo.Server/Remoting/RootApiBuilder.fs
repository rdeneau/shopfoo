namespace Shopfoo.Server.Remoting

open Shopfoo.Shared.Remoting

type RootApiBuilder
    (
        homeApiBuilder: Home.HomeApiBuilder, // ↩
        productApiBuilder: Product.ProductApiBuilder
    ) =
    member _.Build() : RootApi = { // ↩
        Home = homeApiBuilder.Build()
        Product = productApiBuilder.Build()
    }