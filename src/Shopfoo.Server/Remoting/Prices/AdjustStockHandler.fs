namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type AdjustStockHandler(api: FeatApi) =
    inherit SecureCommandHandler<Stock>()

    override _.Handle _ stock user =
        async {
            let! result = api.Product.AdjustStock(stock)
            let response = ResponseBuilder.plain user

            match result with
            | Ok() -> return response.Ok()
            | Error err -> return response.ApiError err
        }