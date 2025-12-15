namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type IndexHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<unit, HomeIndexResponse>()

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

    override _.Handle lang request user =
        async {
            let demoUsers = [
                User.Authorized(userName = "Guest", claims = allFeaturesWithAccess [ Access.View ])
                User.Authorized(userName = "Manager", claims = allFeaturesWithAccess [ Access.View ])
                User.Authorized(
                    userName = "Administrator",
                    claims =
                        allFeaturesWithAccess [
                            Access.View
                            Access.Edit
                            Access.Admin
                        ]
                )
            ]

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations (Feat.Home, user) translations
            return response.Ok { DemoUsers = demoUsers }
        }