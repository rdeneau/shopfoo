namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetProductsHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<Provider, GetProductsResponse>()

    override _.Handle lang request user =
        async {
            let! products = api.Product.GetProducts request.Query

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations
            return response.Ok { Products = products }
        }