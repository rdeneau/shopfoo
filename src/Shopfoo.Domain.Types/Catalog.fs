module Shopfoo.Domain.Types.Catalog

open Shopfoo.Domain.Types.Errors

type Product = {
    SKU: SKU
    Name: string
    Description: string
    ImageUrl: string
}

[<RequireQualifiedAccess>]
module Product =
    module Guard =
        let SKU = GuardCriteria.Create(required = true)
        let Name = GuardCriteria.Create(required = true, maxLength = 128)
        let Description = GuardCriteria.Create(maxLength = 512)
        let ImageUrl = GuardCriteria.None