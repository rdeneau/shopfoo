namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Products
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetProductDetailsHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<SKU, GetProductDetailsResponse>()

    override _.Handle lang request user =
        async {
            let! result = api.Catalog.GetProductDetails(request.Query)

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