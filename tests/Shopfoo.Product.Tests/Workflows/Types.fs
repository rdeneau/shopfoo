module Shopfoo.Product.Tests.Types

open FsCheck
open Shopfoo.Domain.Types.Common
open Shopfoo.Product.Data
open Shopfoo.Product.Data.Helpers
open Shopfoo.Tests.Common.FsCheckArbs

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

module Purchases =
    [<Measure>]
    type daysAgo

    type DaysAgo =
        static member private Within(start: int<daysAgo>, end': int<daysAgo>) = // ↩
            fun (i: int) -> max (start - i * 1<daysAgo>) end'

        static member Before1Y = DaysAgo.Within(start = 730<daysAgo>, end' = 366<daysAgo>)
        static member Within1Y = DaysAgo.Within(start = 365<daysAgo>, end' = 0<daysAgo>)

    type PurchaseUndated = {
        Quantity: int
        Price: Money
    } with
        member purchase.ToStockEvent(daysAgo: int<daysAgo>) = // ↩
            purchase.Quantity |> Units.Purchased purchase.Price (daysAgo |> int |> Helpers.daysAgo)

    let (|ManyPurchasesInEuros|) (NonEmptyArray amountsWithQuantities) : PurchaseUndated list = [
        for PositiveInt qty, PositiveEuros price in amountsWithQuantities do
            { Quantity = qty; Price = price }
    ]

    let (|ManyPurchasesInDollars|) (NonEmptyArray amountsWithQuantities) : PurchaseUndated list = [
        for PositiveInt qty, PositiveDollars price in amountsWithQuantities do
            { Quantity = qty; Price = price }
    ]