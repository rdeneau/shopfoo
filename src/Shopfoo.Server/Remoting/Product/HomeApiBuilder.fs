namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type ProductApiBuilder(api: FeatApi) =
    static let AuthorizedPageCodes = Set [ PageCode.Shared; PageCode.Product ]

    member _.Build() : ProductApi = {
        Index =
            IndexHandler(api, AuthorizedPageCodes) // ↩
            |> Security.authorizeHandler [ Feat.Catalog, Access.View ]
    }