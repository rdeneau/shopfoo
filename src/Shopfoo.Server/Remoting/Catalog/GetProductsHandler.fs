namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetProductsHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<unit, GetProductsResponse>()

    override _.Handle lang request user =
        async {
            let! products = api.Product.GetProducts()

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations
            return response.Ok { Products = products }
        }