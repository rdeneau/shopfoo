namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type RemoveListPriceHandler(api: FeatApi) =
    inherit SecureCommandHandler<SKU>()

    override _.Handle _ sku user =
        async {
            let! result = api.Product.RemoveListPrice(sku)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok() -> response.Ok()
        }