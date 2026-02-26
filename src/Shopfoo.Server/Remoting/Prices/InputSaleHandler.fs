namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Sales
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type InputSaleHandler(api: FeatApi) =
    inherit SecureCommandHandler<Sale>()

    override _.Handle _ sale user =
        async {
            let! result = api.Product.AddSale sale
            let response = ResponseBuilder.plain user

            match result with
            | Ok() -> return response.Ok()
            | Error err -> return response.ApiError err
        }