namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type MarkAsSoldOutHandler(api: FeatApi) =
    inherit SecureCommandHandler<PriceCommand>()

    override _.Handle _ command user =
        async {
            let! result = api.Product.MarkAsSoldOut(command.SKU)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok() -> response.Ok()
        }