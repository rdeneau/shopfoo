namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type ReceiveSupplyHandler(api: FeatApi) =
    inherit SecureCommandHandler<ReceiveSupplyInput>()

    override _.Handle _ _input user =
        async {
            // TODO: implement ReceiveSupply
            let response = ResponseBuilder.plain user
            return response.Ok()
        }