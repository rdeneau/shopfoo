[<AutoOpen>]
module Shopfoo.Domain.Types.Common

open System.Collections.Generic
open Shopfoo.Common
open Shopfoo.Domain.Types.Errors

[<RequireQualifiedAccess>]
type Symbol =
    | Left of string
    | Right of string

type Currency =
    | EUR
    | USD

    member this.Symbol =
        match this with
        | EUR -> Symbol.Right "€"
        | USD -> Symbol.Left "$"

type Money =
    | Dollars of value: decimal
    | Euros of value: decimal

    static member ByCurrency currency value =
        match currency with
        | Currency.USD -> Dollars value
        | Currency.EUR -> Euros value

    member this.Currency =
        match this with
        | Dollars _ -> Currency.USD
        | Euros _ -> Currency.EUR

    member this.Value =
        match this with
        | Dollars value -> value
        | Euros value -> value

    member this.WithValue(value) =
        match this with
        | Dollars _ -> Dollars value
        | Euros _ -> Euros value

    static member Format (currency: Currency) (value: decimal) =
        match currency.Symbol with
        | Symbol.Left symbol -> $"%s{symbol} %.2f{value}"
        | Symbol.Right symbol -> $"%.2f{value} %s{symbol}"

    member this.ValueWithCurrencySymbol = Money.Format this.Currency this.Value

[<RequireQualifiedAccess>]
module Money =
    let private (|ValueWithCase|) =
        function
        | Dollars value -> value, Dollars
        | Euros value -> value, Euros

    /// Apply the computation with the 2 given moneys if they are of the same case (meaning the same currency).
    let tryCompute (ValueWithCase(x, cx)) (ValueWithCase(y, cy)) f = // ↩
        if cx 0m = cy 0m then Some(cx (f x y)) else None

[<RequireQualifiedAccess>]
type Lang =
    | English
    | French
    | Latin

// ⚠️ SKU is a record, not an interface, due to Fable.Remoting limitations.
/// <summary>
/// Stock Keeping Unit
/// </summary>
/// <remarks>
/// Common abstraction for <c>FSID</c>, <c>ISBN</c>, <c>OLID</c>, and <c>SKUUnknown</c>.
/// Defined as a record instead of an interface to overcome serialization issue with Fable.Remoting V5 (that does not accept custom Coders anymore)!
/// Use <c>AsSKU</c> property to convert to SKU from <c>FSID</c>, <c>ISBN</c>, <c>OLID</c>, and <c>SKUUnknown</c>.
/// </remarks>
type SKU = {
    Type: SKUType
    Value: string
} with
    member this.Match(withFSID, withISBN, withOLID) =
        match this.Type with
        | SKUType.FSID fsid -> withFSID fsid
        | SKUType.ISBN isbn -> withISBN isbn
        | SKUType.OLID olid -> withOLID olid
        | SKUType.Unknown -> failwith "SKU type is unknown"

/// FakeStore Product Identifier
and FSID = FSID of int

/// International Standard Book Number
and ISBN = ISBN of string

/// OpenLibrary Identifier
and OLID = OLID of string

and SKUUnknown = | SKUUnknown

and [<RequireQualifiedAccess>] SKUType =
    | FSID of FSID
    | ISBN of ISBN
    | OLID of OLID
    | Unknown

/// Separate extension properties to avoid:
/// - infinite recursion when trying to convert between FSID, ISBN, OLID and SKU
/// - to serialize them and probable issues when trying to do it
[<AutoOpen>]
module SKUExtensions =
    type FSID with
        member this.Value = let (FSID fsid) = this in $"FS-%i{fsid}"
        member this.AsSKU = { Type = SKUType.FSID this; Value = this.Value }

    type ISBN with
        member this.Value = let (ISBN isbn) = this in isbn
        member this.AsSKU = { Type = SKUType.ISBN this; Value = this.Value }

    type OLID with
        member this.Value = let (OLID v) = this in v
        member this.AsSKU = { Type = SKUType.OLID this; Value = this.Value }

    type SKUUnknown with
        member this.Value = String.empty
        member this.AsSKU = { Type = SKUType.Unknown; Value = this.Value }

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Dictionary =
    /// <summary>
    /// Create a <c>Dictionary</c> from the given <paramref name="items"/>,
    /// using <paramref name="getKey"/> to determine the key for each item.
    /// </summary>
    /// <param name="getKey">Determine the key for each item.</param>
    /// <param name="items">Items to put in the dictionary</param>
    let ofListBy getKey items =
        Dictionary<'k, 'v>(
            seq {
                for item in items do
                    KeyValuePair.Create(getKey item, item)
            }
        )

    let tryUpdateBy getKey item (dict: Dictionary<'k, 'v>) =
        let key = getKey item

        let result =
            dict.TryGetValue key // ↩
            |> Option.ofPair
            |> Result.requireSome $"%A{key}"
            |> Result.ignore

        match result with
        | Ok() -> dict[key] <- item
        | Error _ -> ()

        result

#endif