namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type ProductApiBuilder(api: FeatApi) =
    static let AuthorizedPageCodes =
        Set [
            PageCode.Home
            PageCode.Login
            PageCode.Product
        ]

    member _.Build() : ProductApi = {
        GetProducts =
            GetProductsHandler(api, AuthorizedPageCodes) // ↩
            |> Security.authorizeHandler (Claims.single Feat.Catalog Access.View)
        GetProduct =
            GetProductHandler(api, AuthorizedPageCodes) // ↩
            |> Security.authorizeHandler (Claims.single Feat.Catalog Access.View)
        GetPrices =
            GetPricesHandler(api) // ↩
            |> Security.authorizeHandler (Claims.single Feat.Sales Access.View)
        SaveProduct =
            SaveProductHandler(api) // ↩
            |> Security.authorizeHandler (Claims.single Feat.Catalog Access.Edit)
    }