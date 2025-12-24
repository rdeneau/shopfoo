namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type HomeApiBuilder(api: FeatApi) =
    static let pages =
        Set [ // ↩
            PageCode.Home
            PageCode.Login
        ]

    member _.Build() : HomeApi = {
        Index = IndexHandler(api, pages) |> Security.authorizeHandler Claims.none
        GetTranslations = GetTranslationsHandler(api) |> Security.authorizeHandler Claims.none
    }