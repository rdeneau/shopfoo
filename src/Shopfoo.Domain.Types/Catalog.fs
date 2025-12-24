module Shopfoo.Domain.Types.Catalog

open Shopfoo.Domain.Types.Errors

type ImageUrl = {
    Url: string
    Broken: bool
} with
    static member Valid(url) : ImageUrl = { Url = url; Broken = false }
    static member None : ImageUrl = { Url = ""; Broken = true }

type Product = {
    SKU: SKU
    Name: string
    Description: string
    ImageUrl: ImageUrl
}

[<RequireQualifiedAccess>]
module Product =
    module Guard =
        let SKU = GuardCriteria.Create(required = true)
        let Name = GuardCriteria.Create(required = true, maxLength = 128)
        let Description = GuardCriteria.Create(maxLength = 512)
        let ImageUrl = GuardCriteria.None