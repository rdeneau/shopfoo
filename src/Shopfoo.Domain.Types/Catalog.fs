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
    type GuardCriteria =
        static member val SKU = [ NotEmpty ]
        static member val Name = [ NotEmpty; MaxLength 128 ]
        static member val Description = [ NotEmpty ]
        static member val ImageUrl = [ NotEmpty ]