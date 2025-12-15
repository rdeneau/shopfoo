[<RequireQualifiedAccess>]
module Shopfoo.Server.Feat.Catalog

open Shopfoo.Domain.Types.Errors
open Shopfoo.Shared.Remoting

type Api() =
    member _.GetProducts() : Async<Result<Product list, Error>> =
        async {
            return
                Ok [
                // TODO
                ]
        }