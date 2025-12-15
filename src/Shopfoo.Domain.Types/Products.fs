module Shopfoo.Domain.Types.Products

type SKU =
    | SKU of string
    member this.Value = let (SKU value) = this in value

type Product = {
    SKU: SKU
    Name: string
    Description: string
    // TODO: [Product] add image url
}