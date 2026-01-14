module Shopfoo.Client.Filters

open System
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

type Row = {
    Index: int
    Product: Product
    Provider: Provider
}

type private RowBuilder() =
    let index = AutoIndex()

    member _.Build(product, provider) : Row = {
        Index = index.Next()
        Product = product
        Provider = provider
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

    let apply products provider (translations: AppTranslations) (filters: Filters) =
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
                let isSearchTermFound =
                    match filters.SearchTerm with
                    | None -> true
                    | Some searchTerm ->
                        let fieldsToSearch = [
                            product.Description
                            product.Title
                            product.SKU.Value

                            match product.Category with
                            | Category.Bazaar _ -> ()
                            | Category.Books book ->
                                book.Subtitle

                                for author in book.Authors do
                                    author.Name

                                for tag in book.Tags do
                                    tag
                        ]

                        fieldsToSearch |> List.exists _.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase)

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

                if isSearchTermFound && isAuthorMatched && isTagMatched && isBazaarCategoryMatched then
                    rowBuilder.Build(product, provider)
        ]

        sortProducts rows