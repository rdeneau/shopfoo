namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetPurchasePricesHandler(api: FeatApi) =
    inherit SecureQueryHandler<SKU, GetPurchasePricesResponse>()

    override _.Handle _ sku user =
        async {
            let! stats = api.Product.GetPurchasePrices(sku)
            let response = ResponseBuilder.plain user
            return response.Ok { Stats = stats }
        }