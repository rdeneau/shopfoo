[<AutoOpen>]
module Shopfoo.Domain.Types.Common

[<Measure>]
type euros

[<RequireQualifiedAccess>]
type Lang =
    | English
    | French
    | Latin

type SKU =
    | SKU of string
    member this.Value = let (SKU value) = this in value