namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetSalesStatsHandler(api: FeatApi) =
    inherit SecureQueryHandler<SKU, GetSalesStatsResponse>()

    override _.Handle _ sku user =
        async {
            let! stats = api.Product.GetSalesStats(sku)
            let response = ResponseBuilder.plain user
            return response.Ok { Stats = stats }
        }