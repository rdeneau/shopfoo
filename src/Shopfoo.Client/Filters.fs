module Shopfoo.Client.Filters

open System.Text.RegularExpressions
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

/// Properties by which products can be sorted
[<RequireQualifiedAccess>]
type ProductSort =
    | Num
    | Title
    | BookTags
    | BookAuthors
    | StoreCategory
    | SKU

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

type AutoIndex() =
    let mutable index = -1

    member _.Next() =
        index <- index + 1
        index

type MatchType =
    | TextMatch
    | NoMatch

type MatchTexts<'a> = {
    Source: 'a
    GetText: 'a -> string
    Matches: (MatchType * string) list
} with
    member this.Text = this.GetText this.Source
    member this.HasMatches = this.Matches |> List.exists (fun (matchType, _) -> matchType = TextMatch)

type MatchTexts =
    static member init(source, getText) : MatchTexts<_> = {
        Source = source
        GetText = getText
        Matches = []
    }

    static member init(source) : MatchTexts<string> = MatchTexts.init (source, id)

type SearchMatch = {
    Description: MatchTexts<string>
    Title: MatchTexts<string>
    SKU: MatchTexts<SKU>
    BazaarCategory: MatchTexts<BazaarCategory> option
    BookSubtitle: MatchTexts<string> option
    BookAuthors: MatchTexts<BookAuthor> list
    BookTags: MatchTexts<string> list
} with
    member this.HasMatches =
        this.Description.HasMatches
        || this.Title.HasMatches
        || this.SKU.HasMatches
        || (this.BazaarCategory |> Option.toList |> List.exists _.HasMatches)
        || (this.BookSubtitle |> Option.toList |> List.exists _.HasMatches)
        || (this.BookAuthors |> List.exists _.HasMatches)
        || (this.BookTags |> List.exists _.HasMatches)

type Row = {
    Index: int
    Product: Product
    Provider: Provider
    SearchMatch: SearchMatch
}

type private RowBuilder() =
    let index = AutoIndex()

    member _.Build(product, provider, searchMatch) : Row = {
        Index = index.Next()
        Product = product
        Provider = provider
        SearchMatch = searchMatch
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
    SearchTerm: string option
    SortBy: (ProductSort * SortDirection) option
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
        SearchTerm = None
        SortBy = None
    }

    // TODO RDE: unit tests Filters.apply
    let apply (products: Product list) (provider: Provider) (translations: AppTranslations) (filters: Filters) : Row list =
        let getSortKey (row: Row) = [
            match filters.SortBy with
            | None -> ()
            | Some(ProductSort.Num, _) -> SortKeyPart.Num row.Index
            | Some(ProductSort.SKU, _) -> SortKeyPart.Text row.Product.SKU.Value
            | Some(ProductSort.Title, _) -> SortKeyPart.Text row.Product.Title

            | Some(ProductSort.BookAuthors, _) ->
                match row.Product.Category with
                | Category.Books book ->
                    for author in book.Authors do
                        SortKeyPart.Text author.Name
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | Some(ProductSort.BookTags, _) ->
                match row.Product.Category with
                | Category.Books book ->
                    for tag in book.Tags do
                        SortKeyPart.Text tag
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | Some(ProductSort.StoreCategory, _) ->
                match row.Product.Category with
                | Category.Bazaar storeProduct -> SortKeyPart.Text(translations.Product.StoreCategoryOf storeProduct.Category)
                | _ -> ()

                SortKeyPart.Text row.Product.Title
        ]

        let sortProducts =
            match filters.SortBy with
            | None -> id
            | Some(_, Ascending) -> List.sortBy getSortKey
            | Some(_, Descending) -> List.sortByDescending getSortKey

        let rowBuilder = RowBuilder()

        let rows = [
            for product in products do
                let bazaarCategory, book =
                    match product.Category with
                    | Category.Bazaar storeProduct -> Some storeProduct.Category, None
                    | Category.Books book -> None, Some book

                let searchMatchToFill = {
                    Description = MatchTexts.init product.Description
                    Title = MatchTexts.init product.Title
                    SKU = MatchTexts.init (product.SKU, _.Value)
                    BazaarCategory = bazaarCategory |> Option.map (fun x -> MatchTexts.init (x, translations.Product.StoreCategoryOf))
                    BookSubtitle = book |> Option.map (fun b -> MatchTexts.init b.Subtitle)
                    BookAuthors = [
                        match book with
                        | None -> ()
                        | Some book ->
                            for author in book.Authors do
                                MatchTexts.init (author, _.Name)
                    ]
                    BookTags = [
                        match book with
                        | None -> ()
                        | Some book ->
                            for tag in book.Tags do
                                MatchTexts.init tag
                    ]
                }

                let searchMatch =
                    match filters.SearchTerm with
                    | None -> searchMatchToFill
                    | Some searchTerm ->
                        let pattern = Regex.Escape(searchTerm)
                        let options = RegexOptions.IgnoreCase ||| RegexOptions.Multiline

                        let fillMatches (matchToFill: MatchTexts<_>) : MatchTexts<_> =
                            let parts = Regex.Split(matchToFill.Text, pattern, options)
                            let matches = Regex.Matches(matchToFill.Text, pattern, options) |> Seq.toArray

                            {
                                matchToFill with
                                    Matches = [
                                        for i in 0 .. parts.Length - 1 do
                                            NoMatch, parts[i]

                                            if i < matches.Length then
                                                TextMatch, matches[i].Value
                                    ]
                            }

                        {
                            Description = searchMatchToFill.Description |> fillMatches
                            Title = searchMatchToFill.Title |> fillMatches
                            SKU = searchMatchToFill.SKU |> fillMatches
                            BazaarCategory = searchMatchToFill.BazaarCategory |> Option.map fillMatches
                            BookSubtitle = searchMatchToFill.BookSubtitle |> Option.map fillMatches
                            BookAuthors = searchMatchToFill.BookAuthors |> List.map fillMatches
                            BookTags = searchMatchToFill.BookTags |> List.map fillMatches
                        }

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
                    (filters.SearchTerm.IsNone || searchMatch.HasMatches)
                    && isAuthorMatched
                    && isTagMatched
                    && isBazaarCategoryMatched
                then
                    rowBuilder.Build(product, provider, searchMatch)
        ]

        sortProducts rows