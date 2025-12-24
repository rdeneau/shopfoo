namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetPricesHandler(api: FeatApi) =
    inherit SecureQueryHandler<SKU, GetPricesResponse>()

    override _.Handle _ sku user =
        async {
            let! prices = api.Product.GetPrices(sku)
            let response = ResponseBuilder.plain user
            return response.Ok { Prices = prices }
        }