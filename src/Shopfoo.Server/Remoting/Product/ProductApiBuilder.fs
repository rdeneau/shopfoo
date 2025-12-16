namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type ProductApiBuilder(api: FeatApi) =
    static let AuthorizedPageCodes =
        Set [
            PageCode.About
            PageCode.Home
            PageCode.Login
            PageCode.Product
        ]

    member _.Build() : ProductApi = {
        GetProducts =
            GetProductsHandler(api, AuthorizedPageCodes) // ↩
            |> Security.authorizeHandler (Claims.single Feat.Catalog Access.View)
        GetProductDetails =
            GetProductDetailsHandler(api, AuthorizedPageCodes) // ↩
            |> Security.authorizeHandler (Claims.single Feat.Catalog Access.View)
    }