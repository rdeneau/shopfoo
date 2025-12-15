namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetProductsHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<unit, GetProductsResponse>()

    override _.Handle lang request user =
        async {
            let! result = api.Catalog.GetProducts()

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations (Feat.Catalog, user) translations

            return
                match result with
                | Error error -> response.ApiError error
                | Ok products -> response.Ok { Products = products }
        }