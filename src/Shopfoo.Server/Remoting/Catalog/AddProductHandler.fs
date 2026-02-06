namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type AddProductHandler(api: FeatApi) =
    inherit SecureCommandHandler<Product>()

    override _.Handle _ product user =
        async {
            let! result = api.Product.AddProduct(product, Currency.EUR) // TODO: choose currency in the Frontend
            let response = ResponseBuilder.plain user

            return
                match result with
                | Error error -> response.ApiError error
                | Ok() -> response.Ok()
        }