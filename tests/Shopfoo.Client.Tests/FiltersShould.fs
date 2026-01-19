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

type FiltersShould() =
    static let translations: Translations = { Pages = Map Translations.repository[Lang.English] }
    static let appTranslations: AppTranslations = AppTranslations().Fill(translations)

    let sort direction products =
        match direction with
        | SortDirection.Ascending -> products |> List.sort
        | SortDirection.Descending -> products |> List.sortDescending

    let performSortBy (column, direction) f (provider, products) =
        let filters = { Filters.none with SortBy = Some(column, direction) }
        let filteredProducts = filters |> Filters.apply products provider appTranslations |> List.map _.Product
        let filteredValues = filteredProducts |> List.map f
        let expectedValues = products |> List.map f |> sort direction
        filteredValues, expectedValues

    let verifySortBy column f (provider, products) =
        let actualAsc, expectedAsc = performSortBy (column, SortDirection.Ascending) f (provider, products)
        let actualDesc, expectedDesc = performSortBy (column, SortDirection.Descending) f (provider, products)
        let actual = {| Asc = actualAsc; Desc = actualDesc |}
        let expected = {| Asc = expectedAsc; Desc = expectedDesc |}
        actual =! expected

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

    [<Test; ShopfooFsCheckProperty>]
    member _.``index products``(Products(provider, products)) =
        let rows = Filters.none |> Filters.apply products provider appTranslations
        let actual = {| Products = rows |> List.map _.Product; Indexes = rows |> List.map _.Index |}
        let expected = {| Products = products; Indexes = [ 0 .. products.Length - 1 ] |}
        actual =! expected

    [<Test; ShopfooFsCheckProperty>]
    member _.``filter bazaar products by category``(bazaarCategory, BazaarProducts(Products(provider, products))) =
        let filters = { Filters.none with CategoryFilters = Some(CategoryFilters.Bazaar(Some bazaarCategory)) }
        let rows = filters |> Filters.apply products provider appTranslations

        let unexpectedRows =
            rows
            |> List.choose (fun row ->
                match row.Product.Category with
                | Category.Bazaar cat when cat.Category <> bazaarCategory -> Some(row.Index, row.Product)
                | _ -> None
            )

        unexpectedRows =! []

    [<Test; ShopfooFsCheckProperty>]
    member _.``filter books by author``(RandomFromSeed random, BooksProducts(Products(provider, products))) =
        let author =
            products
            |> List.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Authors
                | _ -> []
            )
            |> List.randomChoiceWith random

        let filters = { Filters.none with CategoryFilters = Some(CategoryFilters.Books(Some author.OLID, tag = None)) }
        let rows = filters |> Filters.apply products provider appTranslations

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
    member _.``filter books by tag``(RandomFromSeed random, BooksProducts(Products(provider, products))) =
        let tag =
            products
            |> List.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Tags
                | _ -> []
            )
            |> List.randomChoiceWith random

        let filters = { Filters.none with CategoryFilters = Some(CategoryFilters.Books(authorId = None, tag = Some tag)) }
        let rows = filters |> Filters.apply products provider appTranslations

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
    member _.``search products by title, case insensitive``(caseChange: CaseChange, RandomFromSeed random, Products(provider, products)) =
        let term =
            products
            |> Seq.collect _.Title.Split(" ")
            |> Seq.filter (fun s -> s.Length >= 2)
            |> Seq.randomChoiceWith random
            |> caseChange.Apply

        let filters = { Filters.none with SearchTerm = Some term }
        let rows = filters |> Filters.apply products provider appTranslations
        let rowsMatchedByTitle = rows |> List.filter _.SearchMatch.Title.HasMatches

        // Verify Title
        let unexpectedRows =
            rowsMatchedByTitle
            |> List.filter (fun row -> not (row.Product.Title.Contains(term, StringComparison.OrdinalIgnoreCase)))

        test <@ unexpectedRows |> List.isEmpty @>

        // Verify Matches
        let unexpectedRows =
            rowsMatchedByTitle
            |> List.choose (fun row ->
                let unexpectedMatches =
                    row.SearchMatch.Title.Matches
                    |> List.filter (fun (actualMatchType, text) ->
                        let expectedMatchType =
                            if text.Contains(term, StringComparison.OrdinalIgnoreCase) then
                                TextMatch
                            else
                                NoMatch

                        actualMatchType <> expectedMatchType
                    )

                match unexpectedMatches with
                | [] -> None
                | _ ->
                    Some {|
                        Index = row.Index
                        Title = row.Product.Title
                        SearchTerm = term
                        UnexpectedMatches = unexpectedMatches
                    |}
            )

        test <@ unexpectedRows |> List.isEmpty @>

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort products by sku``(Products(provider, products)) = // ↩
        (provider, products) |> verifySortBy ProductSort.SKU _.SKU.Value

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort products by title``(Products(provider, products)) = // ↩
        (provider, products) |> verifySortBy ProductSort.Title _.Title

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort bazaar products by category (and title)``(BazaarProducts(Products(provider, products))) =
        (provider, products) |> verifySortBy ProductSort.StoreCategory bazaarCategoryAndTitle

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort books by authors (and title)``(BooksProducts(Products(provider, products))) =
        (provider, products) |> verifySortBy ProductSort.BookAuthors bookAuthorsAndTitle

    [<Test; ShopfooFsCheckProperty>]
    member _.``sort books by tags (and title)``(BooksProducts(Products(provider, products))) =
        (provider, products) |> verifySortBy ProductSort.BookTags bookTagsAndTitle