module Shopfoo.Product.Tests.Types

open FsCheck

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