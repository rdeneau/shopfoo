namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type HomeApiBuilder(api: FeatApi) =
    static let AuthorizedPageCodes =
        Set [ // ↩
            PageCode.Home
            PageCode.Login
        ]

    member _.Build() : HomeApi = {
        Index =
            IndexHandler(api, AuthorizedPageCodes) // ↩
            |> Security.authorizeHandler Claims.none
        GetTranslations =
            GetTranslationsHandler(api) // ↩
            |> Security.authorizeHandler Claims.none
    }