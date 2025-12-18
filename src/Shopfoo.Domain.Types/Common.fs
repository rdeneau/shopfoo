[<AutoOpen>]
module Shopfoo.Domain.Types.Common

type Money =
    | Dollars of value: decimal
    | Euros of value: decimal

[<RequireQualifiedAccess>]
type Lang =
    | English
    | French
    | Latin

type SKU =
    | SKU of string
    member this.Value = let (SKU value) = this in value