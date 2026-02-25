namespace Shopfoo.Tests.Common.FsCheckArbs

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

    /// Valid SKU: Value consistent with Type
    let genSKU: Gen<SKU> =
        Gen.oneof [
            genFSID |> Gen.map _.AsSKU // ↩
            genISBN |> Gen.map _.AsSKU
        ]

    /// Real OLID: OL prefix + positive integer + M suffix
    let genOLID: Gen<OLID> =
        Gen.choose (10000, 500000) // ↩
        |> Gen.map (fun i -> OLID $"OL%i{i}M")

    let genMultiWords maxCount : Gen<string> =
        gen {
            let! count = Gen.choose (1, maxCount)
            let! words = genAlphaNumString |> Gen.map _.Value |> Gen.listOfLength count
            return words |> String.concat " "
        }

    let genBazaarProduct: Gen<BazaarProduct> =
        gen {
            let! fsid = genFSID
            let! category = arbMap |> ArbMap.generate
            return { FSID = fsid; Category = category }
        }

    let genAuthor: Gen<BookAuthor> =
        gen {
            let! olid = genOLID
            let! name = genMultiWords 3
            return { OLID = olid; Name = name }
        }

    let genBook: Gen<Book> =
        gen {
            let! isbn = genISBN

            let! subtitle =
                Gen.frequency [ // ↩
                    2, Gen.constant String.Empty
                    8, genMultiWords 7
                ]

            let! authorsCount = Gen.choose (1, 3)
            let! authors = genAuthor |> Gen.listOfLength authorsCount

            let! tagsCount = Gen.choose (0, 5)
            let! tags = genAlphaNumString |> Gen.map _.Value |> Gen.listOfLength tagsCount

            return {
                ISBN = isbn
                Subtitle = subtitle
                Authors = Set authors
                Tags = Set tags
            }
        }

    let genProduct sku category : Gen<Product> =
        gen {
            let! title = genMultiWords 5
            let! description = genMultiWords 20

            return {
                SKU = sku
                Title = title
                Description = description
                Category = category
                ImageUrl = ImageUrl.None
            }
        }

    let genProductByProvider provider : Gen<Product> =
        match provider with
        | Provider.FakeStore ->
            gen {
                let! bazaarProduct = genBazaarProduct
                let! product = genProduct bazaarProduct.FSID.AsSKU (Category.Bazaar bazaarProduct)
                return product
            }
        | Provider.OpenLibrary ->
            gen {
                let! book = genBook
                let! product = genProduct book.ISBN.AsSKU (Category.Books book)
                return product
            }

    type Products = Products of Provider * Product list

    let genProductsByProvider provider : Gen<Products> =
        gen {
            let! count = Gen.choose (3, 20)
            let! products = genProductByProvider provider |> Gen.listOfLength count
            return Products(provider, products)
        }

    let genProducts: Gen<Products> =
        gen {
            let! provider = arbMap |> ArbMap.generate
            return! genProductsByProvider provider
        }

    let shrinkProducts (Products(provider, products)) : seq<Products> =
        let listArb = arbMap |> ArbMap.arbitrary<Product list>

        listArb.Shrinker products
        |> Seq.filter (fun shrunkList -> shrunkList.Length >= 3)
        |> Seq.map (fun shrunkList -> Products(provider, shrunkList))

    type BazaarProducts = BazaarProducts of Products

    let genBazaarProducts: Gen<BazaarProducts> =
        gen {
            let! products = genProductsByProvider Provider.FakeStore
            return BazaarProducts products
        }

    type BooksProducts = BooksProducts of Products

    let genBooksProducts: Gen<BooksProducts> =
        gen {
            let! products = genProductsByProvider Provider.OpenLibrary
            return BooksProducts products
        }

[<AutoOpen>]
module ActivePatterns =
    let (|PositiveDollars|) (PositiveInt amount) : Money = Money.Dollars(decimal amount)
    let (|PositiveEuros|) (PositiveInt amount) : Money = Money.Euros(decimal amount)
    let (|ManyPositiveEuros|) (NonEmptyArray amounts) : Money array = [| for PositiveEuros price in amounts -> price |]

type DomainArbs =
    static member FSID() = Arb.fromGen genFSID
    static member ISBN() = Arb.fromGen genISBN
    static member OLID() = Arb.fromGen genOLID
    static member SKU() = Arb.fromGen genSKU

    static member Products() = Arb.fromGenShrink (genProducts, shrinkProducts)

    static member BazaarProducts() = Arb.fromGen genBazaarProducts
    static member BooksProducts() = Arb.fromGen genBooksProducts