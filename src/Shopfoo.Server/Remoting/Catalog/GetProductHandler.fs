namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Domain.Types
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetProductHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<SKU, GetProductResponse>()

    override _.Handle lang request user =
        async {
            let sku = request.Query
            let! product = api.Product.GetProduct sku

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations
            return response.Ok { Product = product }
        }