namespace Shopfoo.Server.Remoting.Product

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
            let! result = api.Product.GetProduct sku

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations

            return
                match result with
                | Error error -> response.ApiError error
                | Ok product -> response.Ok { Product = product }
        }