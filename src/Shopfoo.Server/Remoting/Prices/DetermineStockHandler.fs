namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type DetermineStockHandler(api: FeatApi) =
    inherit SecureQueryHandler<SKU, Stock>()

    override _.Handle _ sku user =
        async {
            let! stockResult = api.Product.DetermineStock(sku)
            let response = ResponseBuilder.plain user

            match stockResult with
            | Ok stock -> return response.Ok stock
            | Error err -> return response.ApiError err
        }