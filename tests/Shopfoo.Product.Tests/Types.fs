module Shopfoo.Product.Tests.Types

open FsCheck
open Shopfoo.Domain.Types.Common

type CurrencyEnum =
    | EUR = 'e'
    | USD = 'u'

module Currency =
    let (|FromEnum|) (currency: CurrencyEnum) =
        match currency with
        | CurrencyEnum.EUR -> Currency.EUR
        | CurrencyEnum.USD -> Currency.USD
        | _ -> invalidArg "currency" $"Invalid currency: {currency}"

[<RequireQualifiedAccess>]
type WhitespaceChar =
    | Space
    | Nbsp
    | Tab
    | Newline

    member this.Char =
        match this with
        | Space -> ' '
        | Nbsp -> ' '
        | Tab -> '\t'
        | Newline -> '\n'

[<RequireQualifiedAccess>]
type NullOrWhitespace =
    | Null
    | Empty
    | Whitespaces of chars: NonEmptyArray<WhitespaceChar>

type MaxLength = MaxLength of int
type TooLong = TooLong of exceedingChars: NonEmptyArray<char>