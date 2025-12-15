namespace Shopfoo.Server.Remoting.Product

open Shopfoo.Domain.Types.Products
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetProductDetailsHandler(api: FeatApi) =
    inherit SecureQueryHandler<SKU, GetProductDetailsResponse>()

    override _.Handle _ sku user =
        async {
            let! result = api.Catalog.GetProductDetails(sku)

            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok product -> response.Ok { Product = product }
        }