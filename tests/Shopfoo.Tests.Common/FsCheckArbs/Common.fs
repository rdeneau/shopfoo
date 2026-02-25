namespace Shopfoo.Tests.Common.FsCheckArbs

open System
open FsCheck
open FsCheck.FSharp

[<AutoOpen>]
module CommonGen =
    type AlphaNumChar =
        | AlphaNumChar of char
        member this.Value: char = let (AlphaNumChar c) = this in c

    let private AlphaNumChars =
        Set [
            for letter in 'a' .. 'z' do
                letter
                letter |> Char.ToUpperInvariant
            for digit in '0' .. '9' do
                digit
            '_'
        ]

    let genAlphaNumChar: Gen<AlphaNumChar> = Gen.elements AlphaNumChars |> Gen.map AlphaNumChar

    type AlphaNumString =
        | AlphaNumString of string
        member this.Value: string = let (AlphaNumString c) = this in c

    let genAlphaNumString: Gen<AlphaNumString> =
        gen {
            let! len = Gen.choose (1, 32)
            let! chars = genAlphaNumChar |> Gen.listOfLength len
            return chars |> List.map _.Value |> String.Concat |> AlphaNumString
        }

    let shrinkAlphaNumString (AlphaNumString currentStr) : AlphaNumString seq =
        // We reuse the default shrinker for strings...
        // ... BUT we filter the result to guarantee our invariants: not null and only alphanum chars
        ArbMap.defaults.ArbFor<string>().Shrinker currentStr
        |> Seq.filter (fun s -> not (String.IsNullOrWhiteSpace s) && s |> Seq.forall AlphaNumChars.Contains)
        |> Seq.map AlphaNumString

    type OptionFrequency = { NoneWeight: int; SomeWeight: int }

    [<RequireQualifiedAccess>]
    module OptionFrequency =
        let NoneOnceInTenTimes = { NoneWeight = 1; SomeWeight = 9 }

    let genOption frequency (genValue: Gen<'T>) : Gen<'T option> =
        Gen.frequency [
            frequency.NoneWeight, Gen.constant None // ↩
            frequency.SomeWeight, Gen.map Some genValue
        ]

    let genOptionalString: Gen<string option> =
        genAlphaNumString |> Gen.map _.Value |> genOption OptionFrequency.NoneOnceInTenTimes

    let (|RandomFromSeed|) (seed: int) = Random(seed)

type CommonArbs =
    static member AlphaNumString() = Arb.fromGenShrink (genAlphaNumString, shrinkAlphaNumString)