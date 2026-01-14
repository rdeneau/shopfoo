module Shopfoo.Client.Pages.Product.Filters

open Feliz
open Feliz.DaisyUI
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Routing
open Shopfoo.Client.Shared
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

type Row = {
    Index: int
    Product: Product
    Provider: Provider
}

type private RowBuilder() =
    let mutable index = 0

    member _.Build(product, provider) =
        let row = {
            Index = index
            Product = product
            Provider = provider
        }

        index <- index + 1
        row

type Filters = {
    Provider: Provider option
    BooksAuthorId: OLID option
    StoreCategory: StoreCategory option
    SearchTerm: string option
    SortBy: (ProductSort * SortDirection) option
}

[<RequireQualifiedAccess>]
module Filters =
    let apply products provider (translations: AppTranslations) (filters: Filters) =
        let getSortKey (row: Row) = [
            match filters.SortBy with
            | Some(ProductSort.Num, _) -> SortKeyPart.Num row.Index
            | Some(ProductSort.Title, _) -> SortKeyPart.Text row.Product.Title

            | Some(ProductSort.BookAuthors, _) ->
                match row.Product.Category with
                | Category.Books book ->
                    for author in book.Authors do
                        SortKeyPart.Text author.Name
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | Some(ProductSort.StoreCategory, _) ->
                match row.Product.Category with
                | Category.Store storeProduct -> SortKeyPart.Text(translations.Product.StoreCategoryOf storeProduct.Category)
                | _ -> ()

                SortKeyPart.Text row.Product.Title

            | _ -> ()
        ]

        let sortProducts =
            match filters.SortBy with
            | None -> id
            | Some(_, Ascending) -> List.sortBy getSortKey
            | Some(_, Descending) -> List.sortByDescending getSortKey

        let filteredProducts = [
            let rowBuilder = RowBuilder()

            for product in products do
                let isSearchTermMatched =
                    match filters.SearchTerm with
                    | None -> true
                    | Some searchTerm ->
                        let searchTermLower = searchTerm.ToLowerInvariant()

                        let matchesSearchTerm (s: string) =
                            s.ToLowerInvariant().Contains(searchTermLower)

                        let subtitle =
                            match provider, product.Category with
                            | OpenLibrary, Category.Books book -> book.Subtitle
                            | _ -> String.empty

                        // TODO RDE: consider searching in authors, sku and tags too
                        (product.Title |> matchesSearchTerm)
                        || (subtitle |> matchesSearchTerm)
                        || (product.Description |> matchesSearchTerm)

                let isAuthorMatched =
                    match filters.BooksAuthorId, product.Category with
                    | Some authorId, Category.Books book -> book.Authors |> List.exists (fun author -> author.OLID = authorId)
                    | None, Category.Books _ -> true
                    | _ -> true

                let isStoreCategoryMatched =
                    match filters.StoreCategory, product.Category with
                    | Some storeCategory, Category.Store storeProduct -> storeProduct.Category = storeCategory
                    | None, Category.Store _ -> true
                    | _ -> true

                let isMatched = isSearchTermMatched && isAuthorMatched && isStoreCategoryMatched

                if isMatched then
                    yield rowBuilder.Build(product, provider)
        ]

        sortProducts filteredProducts

// TODO RDE: add tags filter tab too
type private Tab(filters: Filters, translations: AppTranslations) =
    member _.authors products =
        let authors =
            products
            |> List.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Authors
                | _ -> []
            )
            |> List.distinct
            |> List.sortBy _.Name

        let selectedAuthor =
            match filters.BooksAuthorId with
            | Some authorId -> authors |> List.tryFind (fun author -> author.OLID = authorId)
            | None -> None

        let navigateToBooksPage authorId =
            Router.navigatePage (Page.ProductBooks(authorId, filters.SearchTerm, filters.SortBy))

        Filter.FilterTab(
            key = "filter-tab-authors",
            label = translations.Product.Authors,
            iconifyIcon = Some fa6Solid.penFancy,
            items = authors,
            selectedItem = selectedAuthor,
            formatItem = _.Name,
            onSelect = (fun author -> navigateToBooksPage (Some author.OLID)),
            onReset = (fun () -> navigateToBooksPage None)
        )

    member _.provider (provider: Provider) (selectedProvider: Provider option) (selectProvider: Provider -> unit) text iconifyIcon page =
        let key = String.toKebab $"%A{provider}"

        Daisy.tab [
            if selectedProvider = Some provider then
                tab.active
            prop.key $"tab-provider-%s{key}"
            prop.className "gap-2"
            prop.onClick (fun _ ->
                selectProvider provider
                Router.navigatePage page
            )
            prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
        ]

    member _.storeCategory storeCategory text iconifyIcon page =
        let key = String.toKebab $"%A{storeCategory}"

        Daisy.tab [
            if filters.StoreCategory = Some storeCategory then
                tab.active
            prop.key $"tab-store-category-%s{key}"
            prop.className "gap-2"
            prop.onClick (fun _ -> Router.navigatePage page)
            prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
        ]

    // TODO RDE: add checkbox Highlight search terms
    member _.search provider =
        // TODO RDE: fix bug that reset the selected authors when searching
        let setSearchTerm searchTerm =
            match provider with
            | OpenLibrary -> Page.ProductBooks(filters.BooksAuthorId, searchTerm, filters.SortBy)
            | FakeStore -> Page.ProductBazaar(filters.StoreCategory, searchTerm, filters.SortBy)
            |> Router.navigatePage

        Daisy.label.input [
            prop.key "search-box"
            prop.className "ml-2"
            prop.children [
                icon fa6Solid.magnifyingGlass
                Html.input [
                    prop.key "search-input"
                    prop.type' "text"
                    prop.placeholder translations.Home.Search
                    match filters.SearchTerm with
                    | Some value -> prop.value value
                    | None -> prop.value ""
                    prop.onChange (fun (searchTerm: string) ->
                        match String.trimWhiteSpace searchTerm with
                        | "" -> setSearchTerm None
                        | s -> setSearchTerm (Some s)
                    )
                ]
                if filters.SearchTerm.IsSome then
                    Daisy.button.a [
                        prop.key "search-tab-close-button"
                        prop.className "btn btn-ghost btn-sm btn-circle mr-[-8px]"
                        prop.onClick (fun _ -> setSearchTerm None)
                        prop.text "✕"
                    ]
            ]
        ]

[<ReactComponent>]
let IndexFilterBar (filters: Filters) (products: Product list) (selectedProvider: Provider option) selectProvider (translations: AppTranslations) =
    let tab = Tab(filters, translations)

    Daisy.tabs [
        tabs.border
        prop.key "tabs-providers"
        prop.className "pb-2 border-b border-gray-200"
        prop.children [
            tab.provider
                OpenLibrary
                selectedProvider
                selectProvider
                translations.Home.Books
                fa6Solid.book
                (Page.ProductBooks(filters.BooksAuthorId, filters.SearchTerm, filters.SortBy))

            tab.provider
                FakeStore
                selectedProvider
                selectProvider
                translations.Home.Bazaar
                fa6Solid.store
                (Page.ProductBazaar(filters.StoreCategory, filters.SearchTerm, filters.SortBy))

            Daisy.divider [
                divider.horizontal
                prop.key "tabs-divider"
                prop.className "mx-1"
            ]

            match selectedProvider with
            | Some FakeStore ->
                tab.storeCategory
                    StoreCategory.Clothing
                    translations.Product.StoreCategory.Clothing
                    fa6Solid.shirt
                    (Page.ProductBazaar(Some StoreCategory.Clothing, filters.SearchTerm, filters.SortBy))

                tab.storeCategory
                    StoreCategory.Electronics
                    translations.Product.StoreCategory.Electronics
                    fa6Solid.tv
                    (Page.ProductBazaar(Some StoreCategory.Electronics, filters.SearchTerm, filters.SortBy))

                tab.storeCategory
                    StoreCategory.Jewelry
                    translations.Product.StoreCategory.Jewelry
                    fa6Solid.gem
                    (Page.ProductBazaar(Some StoreCategory.Jewelry, filters.SearchTerm, filters.SortBy))

            | Some OpenLibrary -> tab.authors products
            | _ -> ()

            match selectedProvider with
            | Some provider -> tab.search provider
            | None -> ()
        ]
    ]