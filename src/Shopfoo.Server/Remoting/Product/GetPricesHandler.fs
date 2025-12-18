namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetPricesHandler(api: FeatApi) =
    inherit SecureQueryHandler<SKU, GetPricesResponse>()

    override _.Handle _ sku user =
        async {
            let! result = api.Product.GetPrices(sku)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok prices -> response.Ok { Prices = prices }
        }