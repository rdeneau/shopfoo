namespace Shopfoo.Client.Tests.FsCheckArbs

open System
open FsCheck
open FsCheck.FSharp
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog

[<AutoOpen>]
module DomainGen =
    let private arbMap = ArbMap.defaults |> ArbMap.mergeWith<CommonArbs>

    /// Real FSID: containing only positive integers
    let genFSID: Gen<FSID> = Gen.choose (1, 32) |> Gen.map FSID

    /// Real ISBN: 978 prefix + 9 random digits + valid checksum digit
    let genISBN: Gen<ISBN> =
        gen {
            let! middleDigits = (Gen.elements [ 0..9 ]) |> Gen.listOfLength 9
            let digits = 9 :: 7 :: 8 :: middleDigits

            let checksum =
                digits
                |> List.mapi (fun i d -> if i % 2 = 0 then d else d * 3)
                |> List.sum
                |> fun sum -> (10 - (sum % 10)) % 10

            let allDigits = digits @ [ checksum ]
            let isbnString = allDigits |> List.map string |> String.Concat
            return ISBN isbnString
        }

    /// Real OLID: OL prefix + positive integer + M suffix
    let genOLID: Gen<OLID> = Gen.choose (10000, 500000) |> Gen.map (fun i -> OLID $"OL%i{i}M")

    /// Valid SKU: Value consistent with Type
    let genSKU: Gen<SKU> =
        Gen.oneof [
            genFSID |> Gen.map _.AsSKU
            genISBN |> Gen.map _.AsSKU
        ]

type DomainArbs =
    static member FSID() = Arb.fromGen genFSID
    static member ISBN() = Arb.fromGen genISBN
    static member OLID() = Arb.fromGen genOLID
    static member SKU() = Arb.fromGen genSKU