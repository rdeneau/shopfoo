namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetTranslationsHandler(api: FeatApi) =
    inherit SecureQueryHandler<Set<PageCode>, Translations>()

    let features = [
        Feat.Home
        Feat.Catalog
        Feat.Sales
        Feat.Warehouse
    ]

    let allFeaturesWithAccess accesses = [
        for feat in features do
            for access in accesses do
                feat, access
    ]

    override _.Handle lang pageCodes user =
        async {
            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = pageCodes
                    requested = pageCodes
                }

            let translations =
                match translations with
                | Ok translations -> translations
                | Error _ -> Translations.Empty

            let response = ResponseBuilder.plain user
            return response.Ok translations
        }