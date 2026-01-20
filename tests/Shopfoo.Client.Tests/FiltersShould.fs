namespace Shopfoo.Client.Tests

open System
open Shopfoo.Client.Filters
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Translations
open Shopfoo.Home.Data
open Shopfoo.Client.Tests.FsCheckArbs
open Shopfoo.Shared.Translations
open Swensen.Unquote
open TUnit.Core

[<RequireQualifiedAccess>]
type CaseChange =
    | Upper
    | Lower
    | None

    member this.Apply(s: string) =
        match this with
        | Upper -> s.ToUpperInvariant()
        | Lower -> s.ToLowerInvariant()
        | None -> s

type SearchResultCheckFailure =
    | ResultsTextDoesNotContainTerm
    | UnexpectedMatchingResults of {| expected: SearchTargetResult array; actual: SearchTargetResult array |}
    | UnexpectedNumberOfResults of expectedTexts: string array
    | UnexpectedTexts of {| expected: string; actual: string |} list

type FiltersShould() =
    static let translations: Translations = { Pages = Map Translations.repository[Lang.English] }
    static let appTranslations: AppTranslations = AppTranslations().Fill(translations)

    let verifySearch (caseChange: CaseChange) (random: Random) products column (getExpectedTexts: Product -> string array) =
        // Choose a random word
        let term =
            products
            |> Seq.collect getExpectedTexts
            |> Seq.collect _.Split(" ")
            |> Seq.filter (fun s -> s.Length >= 2)
            |> Seq.randomChoiceWith random
            |> caseChange.Apply

        let filters = { Filters.none with SearchColumns = Set [ column ]; SearchTerm = Some term }
        let rows = filters |> Filters.apply products appTranslations

        let unexpectedRows =
            rows
            |> List.choose (fun row ->
                let expectedTexts = getExpectedTexts row.Product
                let searchTargetResults = row.SearchResult[column] |> List.toArray

                let checkFailure =
                    if expectedTexts.Length <> searchTargetResults.Length then
                        Some(UnexpectedNumberOfResults expectedTexts)
                    else
                        let unexpectedTexts = [
                            for expectedText, result in Array.zip expectedTexts searchTargetResults do
                                if not (expectedText.Equals(result.Text, StringComparison.OrdinalIgnoreCase)) then
                                    {| expected = expectedText; actual = result.Text |}
                        ]

                        if not unexpectedTexts.IsEmpty then
                            Some(UnexpectedTexts unexpectedTexts)
                        else
                            let expectedTextMatches = searchTargetResults |> Array.filter _.Text.Contains(term, StringComparison.OrdinalIgnoreCase)

                            if expectedTextMatches.Length = 0 then
                                Some ResultsTextDoesNotContainTerm
                            else
                                let actualTextMatches =
                                    searchTargetResults
                                    |> Array.filter (fun result -> result.Matches |> List.exists (fun m -> m.MatchType = TextMatch))

                                if actualTextMatches <> expectedTextMatches then
                                    Some(UnexpectedMatchingResults {| expected = expectedTextMatches; actual = actualTextMatches |})
                                else
                                    None

                checkFailure
                |> Option.map (fun checkFailure -> {|
                    Row = row
                    SearchTerm = term
                    CheckFailure = checkFailure
                |})
            )

        unexpectedRows =! []

    let sort direction products =
        match direction with
        | SortDirection.Ascending -> products |> List.sort
        | SortDirection.Descending -> products |> List.sortDescending

    let performSortBy (column, direction) (mapActual: Row -> 't) (mapExpected: int -> Product -> 't) products = {|
        actual =
            { Filters.none with SortBy = Some(column, direction) }
            |> Filters.apply products appTranslations
            |> List.map mapActual
        expected =
            products // ↩
            |> List.mapi mapExpected
            |> sort direction
    |}

    let verifyRowSortBy column mapActual mapExpected products =
        let asc = performSortBy (column, SortDirection.Ascending) mapActual mapExpected products
        let desc = performSortBy (column, SortDirection.Descending) mapActual mapExpected products
        let actual = {| Asc = asc.actual; Desc = desc.actual |}
        let expected = {| Asc = asc.expected; Desc = desc.expected |}
        actual =! expected

    let verifyProductSortBy column f products =
        let mapActual row = f row.Product
        let mapExpected _i product = f product
        verifyRowSortBy column mapActual mapExpected products

    let bazaarCategoryAndTitle product =
        match product.Category with
        | Category.Bazaar cat -> cat.Category, product.Title
        | _ -> failwith "Expected only bazaar products"

    let bookAuthorsAndTitle product =
        match product.Category with
        | Category.Books book -> book.Authors |> List.map _.Name, product.Title
        | _ -> failwith "Expected only book products"

    let bookTagsAndTitle product =
        match product.Category with
        | Category.Books book -> book.Tags, product.Title
        | _ -> failwith "Expected only book products"

    let bookSubtitle product =
        match product.Category with
        | Category.Books book -> book.Subtitle
        | _ -> failwith "Expected only book products"

    [<Test; ShopfooFsCheckProperty>]
    member _.``index products``(Products(_, products)) =
        let rows = Filters.none |> Filters.apply products appTranslations
        let actual = {| Products = rows |> List.map _.Product; Indexes = rows |> List.map _.Index |}
        let expected = {| Products = products; Indexes = [ 0 .. products.Length - 1 ] |}
        actual =! expected

    [<Test; ShopfooFsCheckProperty>]
    member _.``filter bazaar products by category``(bazaarCategory, BazaarProducts(Products(_, products))) =
        let filters = { Filters.none with CategoryFilters = Some(CategoryFilters.Bazaar(Some bazaarCategory)) }
        let rows = filters |> Filters.apply products appTranslations

        let unexpectedRows =
            rows
            |> List.choose (fun row ->
                match row.Product.Category with
                | Category.Bazaar cat when cat.Category <> bazaarCategory -> Some(row.Index, row.Product)
                | _ -> None
            )

        unexpectedRows =! []

    [<Test; ShopfooFsCheckProperty>]
    member _.``filter books by author``(RandomFromSeed random, BooksProducts(Products(_, products))) =
        let author =
            products
            |> List.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Authors
                | _ -> []
            )
            |> List.randomChoiceWith random

        let filters = { Filters.none with CategoryFilters = Some(CategoryFilters.Books(Some author.OLID, tag = None)) }
        let rows = filters |> Filters.apply products appTranslations

        let unexpectedRows =
            rows
            |> List.choose (fun row ->
                match row.Product.Category with
                | Category.Books book when not (List.contains author book.Authors) ->
                    Some {|
                        Index = row.Index
                        SKU = row.Product.SKU
                        Authors = book.Authors |> List.map _.OLID
                        ExpectedAuthor = author.OLID
                    |}
                | _ -> None
            )

        unexpectedRows =! []

    [<Test; ShopfooFsCheckProperty>]
    member _.``filter books by tag``(RandomFromSeed random, BooksProducts(Products(_, products))) =
        let tag =
            products
            |> List.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Tags
                | _ -> []
            )
            |> List.randomChoiceWith random

        let filters = { Filters.none with CategoryFilters = Some(CategoryFilters.Books(authorId = None, tag = Some tag)) }
        let rows = filters |> Filters.apply products appTranslations

        let unexpectedRows =
            rows
            |> List.choose (fun row ->
                match row.Product.Category with
                | Category.Books book when not (List.contains tag book.Tags) ->
                    Some {|
                        Index = row.Index
                        SKU = row.Product.SKU
                        Tags = book.Tags
                        ExpectedTag = tag
                    |}
                | _ -> None
            )

        unexpectedRows =! []

    [<Test; ShopfooFsCheckProperty>]
    member _.``search products by description, case insensitive``(caseChange, RandomFromSeed random, Products(_, products)) =
        verifySearch caseChange random products Column.Description (fun product -> [| product.Description |])

    [<Test; ShopfooFsCheckProperty>]
    member _.``search products by title, case insensitive``(caseChange, RandomFromSeed random, Products(_, products)) =
        verifySearch caseChange random products Column.Name (fun product -> [| product.Title |])

    [<Test; ShopfooFsCheckProperty>]
    member _.``search books by subtitle, case insensitive``(caseChange, RandomFromSeed random, BooksProducts(Products(_, products))) =
        verifySearch caseChange random products Column.BookSubtitle
        <| fun product -> [|
            match product.Category with
            | Category.Books book -> book.Subtitle
            | Category.Bazaar _ -> failwith "Expected only book products"
        |]

    [<Test; ShopfooFsCheckProperty>]
    member _.``search books by author, case insensitive``(caseChange, RandomFromSeed random, BooksProducts(Products(_, products))) =
        verifySearch caseChange random products Column.BookAuthors
        <| fun product -> [|
            match product.Category with
            | Category.Books book ->
                for author in book.Authors do
                    author.Name
            | Category.Bazaar _ -> failwith "Expected only book products"
        |]

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort products by num``(Products(_, products)) = // ↩
        products |> verifyRowSortBy Column.Num _.Index (fun i _ -> i)

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort products by sku``(Products(_, products)) = // ↩
        products |> verifyProductSortBy Column.SKU _.SKU.Value

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort products by title``(Products(_, products)) = // ↩
        products |> verifyProductSortBy Column.Name _.Title

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort bazaar products by category (and title)``(BazaarProducts(Products(_, products))) =
        products |> verifyProductSortBy Column.BazaarCategory bazaarCategoryAndTitle

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort books by authors (and title)``(BooksProducts(Products(_, products))) =
        products |> verifyProductSortBy Column.BookAuthors bookAuthorsAndTitle

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort books by tags (and title)``(BooksProducts(Products(_, products))) = // ↩
        products |> verifyProductSortBy Column.BookTags bookTagsAndTitle