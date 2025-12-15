namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type IndexHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<unit, HomeIndexResponse>()

    let coreFeatures = [
        Feat.Home
        Feat.Catalog
        Feat.Sales
        Feat.Warehouse
    ]

    let withAccess access features : Claims =
        Map [ for feat in features -> feat, access ]

    override _.Handle lang request user =
        async {
            let demoUsers = [
                User.Authorized(userName = "Guest", claims = (coreFeatures |> withAccess Access.View))
                User.Authorized(userName = "Manager", claims = (coreFeatures |> withAccess Access.Edit))
                User.Authorized(userName = "Administrator", claims = (Feat.Admin :: coreFeatures |> withAccess Access.Edit))
            ]

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations
            return response.Ok { DemoUsers = demoUsers }
        }