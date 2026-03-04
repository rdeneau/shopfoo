namespace Shopfoo.Server.Remoting.Admin

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type ResetProductCacheHandler(api: FeatApi) =
    inherit SecureCommandHandler<unit>()

    override _.Handle _ () user =
        async {
            let! result = api.Product.ResetAllCaches()
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok() -> response.Ok()
        }