namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Sales
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type SavePricesHandler(api: FeatApi) =
    inherit SecureCommandHandler<Prices>()

    override _.Handle _ prices user =
        async {
            let! result = api.Product.SavePrices(prices)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok() -> response.Ok()
        }