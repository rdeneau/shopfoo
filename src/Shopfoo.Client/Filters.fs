module Shopfoo.Client.Filters

open Shopfoo.Client.Search
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

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
    Search: SearchConfig
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
        Search = {
            Columns = Set.empty
            CaseMatching = CaseInsensitive
            Highlighting = Highlighting.None
            Term = None
        }
        SortBy = None
    }

    let defaults: Filters = {
        none with
            Search = {
                none.Search with
                    Highlighting = Highlighting.Active
                    Columns =
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
    }

    /// Object to operate filtering and sorting on products according to given filters
    type private FiltersOperator(filters: Filters, translations: AppTranslations) =
        let search = Searcher(filters.Search, translations)

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
                    | Set.Empty -> SortKeyPart.Text ""
                    | Set.NotEmpty as authors ->
                        for author in authors do
                            SortKeyPart.Text author.Name
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | Some(Column.BookTags, _) ->
                match row.Product.Category with
                | Category.Books book ->
                    match book.Tags with
                    | Set.Empty -> SortKeyPart.Text ""
                    | Set.NotEmpty as tags ->
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

        member _.filter(products: Product list) : Row list =
            let index = AutoIndex()

            let buildRow product searchResult : Row = {
                Index = index.Next()
                Product = product
                SearchResult = searchResult
            }

            let rows = [
                for product in products do
                    let searchResult = SearchResult.build filters.Search (search.Product product)

                    let isSearchMatched =
                        match searchResult |> SearchResult.status with
                        | SearchStatus.NoMatch -> false
                        | SearchStatus.Matches _ -> true

                    let isAuthorMatched =
                        match filters.BooksAuthorId, product.Category with
                        | Some selectedAuthorId, Category.Books book -> book.Authors |> Set.exists (fun author -> author.OLID = selectedAuthorId)
                        | _ -> true

                    let isTagMatched =
                        match filters.BooksTag, product.Category with
                        | Some selectedTag, Category.Books book -> book.Tags |> Set.exists (fun tag -> tag = selectedTag)
                        | _ -> true

                    let isBazaarCategoryMatched =
                        match filters.BazaarCategory, product.Category with
                        | Some selectedCategory, Category.Bazaar storeProduct -> storeProduct.Category = selectedCategory
                        | _ -> true

                    if
                        (filters.Search.Term.IsNone || isSearchMatched)
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