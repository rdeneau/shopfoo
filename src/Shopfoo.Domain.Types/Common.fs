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

[<Interface>]
type SKU =
    abstract member Type: SKUType
    abstract member Value: string

/// FakeStore Product Identifier
and FSID =
    | FSID of int

    member this.Value =
        let (FSID fsid) = this
        $"FS%i{fsid}"

    interface SKU with
        member this.Type = SKUType.FSID this
        member this.Value = this.Value

/// International Standard Book Number
and ISBN =
    | ISBN of string

    member this.Value =
        let (ISBN isbn) = this
        isbn

    interface SKU with
        member this.Type = SKUType.ISBN this
        member this.Value = this.Value

and SKUUnknown =
    | SKUUnknown
    interface SKU with
        member _.Type = SKUType.Unknown
        member _.Value = String.empty

and [<RequireQualifiedAccess>] SKUType =
    | FSID of FSID
    | ISBN of ISBN
    | Unknown

type SKU with
    static member ParseKey(key: string) : SKU =
        match key |> String.split '-' with
        | [| "FS"; String.Int fsid |] -> FSID fsid
        | [| "BN"; isbn |] -> ISBN isbn
        | _ -> SKUUnknown

    member this.Key =
        match this.Type with
        | SKUType.FSID _ -> $"FS-%s{this.Value}"
        | SKUType.ISBN _ -> $"BN-%s{this.Value}"
        | SKUType.Unknown -> ""

    member this.Match(withFSID, withISBN) =
        match this.Type with
        | SKUType.FSID fsid -> withFSID fsid
        | SKUType.ISBN isbn -> withISBN isbn
        | SKUType.Unknown -> failwith "SKU type is unknown"

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
        Dictionary<'k, 'v>(seq { for item in items -> KeyValuePair.Create(getKey item, item) })

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