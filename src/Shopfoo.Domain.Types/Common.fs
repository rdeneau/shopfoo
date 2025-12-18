[<AutoOpen>]
module Shopfoo.Domain.Types.Common

type SKU =
    | SKU of string
    member this.Value = let (SKU value) = this in value

[<RequireQualifiedAccess>]
type Lang =
    | English
    | French
    | Latin