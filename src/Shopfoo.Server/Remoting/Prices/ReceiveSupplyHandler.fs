namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type ReceiveSupplyHandler(api: FeatApi) =
    inherit SecureCommandHandler<ReceiveSupplyInput>()

    override _.Handle _ input user =
        async {
            let! result = api.Product.ReceiveSupply input
            let response = ResponseBuilder.plain user

            match result with
            | Ok() -> return response.Ok()
            | Error err -> return response.ApiError err
        }