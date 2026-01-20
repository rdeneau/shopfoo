module Shopfoo.Client.Filters

open System.Text.RegularExpressions
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

/// Properties displayed in the table and by which products can be filtered, searched, and/or sorted
[<RequireQualifiedAccess>]
type Column =
    | Num
    | SKU
    | Name
    | Description
    | BazaarCategory
    | BookSubtitle
    | BookAuthors
    | BookTags

type SortDirection =
    | Ascending
    | Descending

    member this.Toggle() =
        match this with
        | Ascending -> Descending
        | Descending -> Ascending

/// Part of a sort key, either numeric or textual
[<RequireQualifiedAccess>]
type SortKeyPart =
    | Num of int
    | Text of string

/// Class generating an index starting from 0 and incrementing on each call to `Next()`
type AutoIndex() =
    let mutable index = -1

    member _.Next() =
        index <- index + 1
        index

type MatchType =
    | TextMatch
    | NoMatch

type MatchText = { MatchType: MatchType; Text: string }

[<RequireQualifiedAccess>]
type SearchTarget =
    | Description of string
    | Title of string
    | SKU of SKU
    | BazaarCategory of BazaarCategory
    | BookSubtitle of string
    | BookAuthor of BookAuthor
    | BookTag of string

type SearchTargetResult = {
    Target: SearchTarget
    Text: string
    Matches: MatchText list
}

type SearchResult = Map<Column, SearchTargetResult list>

[<RequireQualifiedAccess>]
type SearchStatus =
    | NoMatch
    | Matches of Set<Column>

[<RequireQualifiedAccess>]
module SearchResult =
    let build columns f : SearchResult = Map [ for column in columns -> column, f column ]

    let status (searchResult: SearchResult) : SearchStatus =
        searchResult
        |> Map.toList
        |> List.choose (fun (column, results) ->
            let b = results |> Seq.collect _.Matches |> Seq.exists (fun x -> x.MatchType = TextMatch)
            Option.ofPair (b, column)
        )
        |> function
            | [] -> SearchStatus.NoMatch
            | matches -> SearchStatus.Matches(Set.ofList matches)

type Row = {
    Index: int
    Product: Product
    SearchResult: SearchResult
}

[<RequireQualifiedAccess>]
type CategoryFilters =
    | Bazaar of category: BazaarCategory option
    | Books of authorId: OLID option * tag: string option

[<RequireQualifiedAccess>]
module CategoryFilters =
    [<RequireQualifiedAccess>]
    module Defaults =
        let bazaar = CategoryFilters.Bazaar(category = None)
        let books = CategoryFilters.Books(authorId = None, tag = None)

type Filters = {
    CategoryFilters: CategoryFilters option
    SearchColumns: Set<Column>
    SearchTerm: string option
    SortBy: (Column * SortDirection) option
} with
    member this.Provider =
        match this.CategoryFilters with
        | Some(CategoryFilters.Bazaar _) -> Some Provider.FakeStore
        | Some(CategoryFilters.Books _) -> Some Provider.OpenLibrary
        | None -> None

    member this.BazaarCategory =
        match this.CategoryFilters with
        | Some(CategoryFilters.Bazaar category) -> category
        | Some(CategoryFilters.Books _) -> None
        | None -> None

    member this.BooksAuthorId =
        match this.CategoryFilters with
        | Some(CategoryFilters.Books(authorId, _)) -> authorId
        | Some(CategoryFilters.Bazaar _) -> None
        | None -> None

    member this.BooksTag =
        match this.CategoryFilters with
        | Some(CategoryFilters.Books(_, tag)) -> tag
        | Some(CategoryFilters.Bazaar _) -> None
        | None -> None

    member this.ToBazaar() = { this with CategoryFilters = Some(CategoryFilters.Bazaar None) }
    member this.ToBazaarWithCategory category = { this with CategoryFilters = Some(CategoryFilters.Bazaar(Some category)) }

    member this.ToBooks() = { this with CategoryFilters = Some(CategoryFilters.Books(authorId = None, tag = None)) }
    member this.ToBooksWithAuthor authorId = { this with CategoryFilters = Some(CategoryFilters.Books(authorId, this.BooksTag)) }
    member this.ToBooksWithTag tag = { this with CategoryFilters = Some(CategoryFilters.Books(this.BooksAuthorId, tag)) }

[<RequireQualifiedAccess>]
module Filters =
    let none: Filters = {
        CategoryFilters = None
        SearchColumns = Set.empty
        SearchTerm = None
        SortBy = None
    }

    let defaults: Filters = {
        none with
            SearchColumns =
                Set [
                    Column.SKU
                    Column.Name
                    Column.Description
                    Column.BazaarCategory
                    Column.BookSubtitle
                    Column.BookAuthors
                    Column.BookTags
                ]
    }

    type private FiltersOperator(filters: Filters, translations: AppTranslations) =
        let getSortKey (row: Row) = [
            match filters.SortBy with
            | None -> ()
            | Some(Column.Num, _) -> SortKeyPart.Num row.Index
            | Some(Column.SKU, _) -> SortKeyPart.Text row.Product.SKU.Value
            | Some(Column.Name, _) -> SortKeyPart.Text row.Product.Title
            | Some(Column.Description, _) -> SortKeyPart.Text row.Product.Description

            | Some(Column.BazaarCategory, _) ->
                match row.Product.Category with
                | Category.Bazaar storeProduct -> SortKeyPart.Text(translations.Product.StoreCategoryOf storeProduct.Category)
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | Some(Column.BookSubtitle, _) ->
                match row.Product.Category with
                | Category.Books book -> SortKeyPart.Text book.Subtitle
                | _ -> ()

            | Some(Column.BookAuthors, _) ->
                match row.Product.Category with
                | Category.Books book ->
                    match book.Authors with
                    | [] -> SortKeyPart.Text ""
                    | authors ->
                        for author in authors do
                            SortKeyPart.Text author.Name
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | Some(Column.BookTags, _) ->
                match row.Product.Category with
                | Category.Books book ->
                    match book.Tags with
                    | [] -> SortKeyPart.Text ""
                    | tags ->
                        for tag in tags do
                            SortKeyPart.Text tag
                | _ -> ()

                SortKeyPart.Text row.Product.Title
        ]

        let sortProducts =
            match filters.SortBy with
            | None -> id
            | Some(_, Ascending) -> List.sortBy getSortKey
            | Some(_, Descending) -> List.sortByDescending getSortKey

        let searchProductBy (product: Product) (searchTerm: string option) column : SearchTargetResult list = [
            let search (target: SearchTarget) (text: string) : SearchTargetResult = {
                Target = target
                Text = text
                Matches = [
                    match searchTerm with
                    | Some(String.NotEmpty as term) ->
                        let regex = Regex(pattern = Regex.Escape(term), options = (RegexOptions.IgnoreCase ||| RegexOptions.Multiline))
                        let parts = regex.Split(text)
                        let matches = regex.Matches(text) |> Seq.toArray

                        for i in 0 .. parts.Length - 1 do
                            match parts[i] with
                            | String.NullOrWhiteSpace -> ()
                            | part -> { MatchType = NoMatch; Text = part }

                            if i < matches.Length then
                                { MatchType = TextMatch; Text = matches[i].Value }
                    | None
                    | Some _ -> ()
                ]
            }

            let searchText targetCase text = search (targetCase text) text
            let searchObject targetCase object getText = search (targetCase object) (getText object)

            match column with
            | Column.Num -> () // No search on Num column
            | Column.SKU -> searchObject SearchTarget.SKU product.SKU _.Value
            | Column.Name -> searchText SearchTarget.Title product.Title
            | Column.Description -> searchText SearchTarget.Description product.Description
            | Column.BazaarCategory ->
                match product.Category with
                | Category.Bazaar storeProduct -> searchObject SearchTarget.BazaarCategory storeProduct.Category translations.Product.StoreCategoryOf
                | _ -> ()
            | Column.BookSubtitle ->
                match product.Category with
                | Category.Books book -> searchText SearchTarget.BookSubtitle book.Subtitle
                | _ -> ()
            | Column.BookAuthors ->
                match product.Category with
                | Category.Books book ->
                    for author in book.Authors do
                        searchObject SearchTarget.BookAuthor author _.Name
                | _ -> ()
            | Column.BookTags ->
                match product.Category with
                | Category.Books book ->
                    for tag in book.Tags do
                        searchText SearchTarget.BookTag tag
                | _ -> ()
        ]

        member _.filter(products: Product list) : Row list =
            let index = AutoIndex()

            let buildRow product searchResult : Row = {
                Index = index.Next()
                Product = product
                SearchResult = searchResult
            }

            let rows = [
                for product in products do
                    let searchResult = SearchResult.build filters.SearchColumns (searchProductBy product filters.SearchTerm)

                    let isSearchMatched =
                        match searchResult |> SearchResult.status with
                        | SearchStatus.NoMatch -> false
                        | SearchStatus.Matches _ -> true

                    let isAuthorMatched =
                        match filters.BooksAuthorId, product.Category with
                        | Some selectedAuthorId, Category.Books book -> book.Authors |> List.exists (fun author -> author.OLID = selectedAuthorId)
                        | _ -> true

                    let isTagMatched =
                        match filters.BooksTag, product.Category with
                        | Some selectedTag, Category.Books book -> book.Tags |> List.exists (fun tag -> tag = selectedTag)
                        | _ -> true

                    let isBazaarCategoryMatched =
                        match filters.BazaarCategory, product.Category with
                        | Some selectedCategory, Category.Bazaar storeProduct -> storeProduct.Category = selectedCategory
                        | _ -> true

                    if
                        (filters.SearchTerm.IsNone || isSearchMatched)
                        && isAuthorMatched
                        && isTagMatched
                        && isBazaarCategoryMatched
                    then
                        buildRow product searchResult
            ]

            sortProducts rows

    let apply products translations filters =
        let operator = FiltersOperator(filters, translations)
        operator.filter products