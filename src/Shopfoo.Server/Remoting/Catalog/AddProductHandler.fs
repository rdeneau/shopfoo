namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type AddProductHandler(api: FeatApi) =
    inherit SecureCommandHandler<Product>()

    override _.Handle _ product user =
        async {
            let! result = api.Product.AddProduct(product)
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok() -> response.Ok()
        }