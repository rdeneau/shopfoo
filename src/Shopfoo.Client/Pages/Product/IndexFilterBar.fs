module Shopfoo.Client.Pages.Product.Filters

open Feliz
open Feliz.DaisyUI
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Routing
open Shopfoo.Client.Filters
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

type private Tab(filters: Filters, translations: AppTranslations) =
    let reactKeyOf x = String.toKebab $"%A{x}"

    let navigateWithFilters changeFilters = Page.ProductIndex(changeFilters filters) |> Router.navigatePage

    member _.bazaarCategory bazaarCategory iconifyIcon text =
        Daisy.tab [
            if filters.BazaarCategory = Some bazaarCategory then
                tab.active
            prop.key $"tab-store-category-%s{reactKeyOf bazaarCategory}"
            prop.className "gap-2"
            prop.onClick (fun _ -> navigateWithFilters _.ToBazaarWithCategory(bazaarCategory))
            prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
        ]

    member _.booksAuthors products =
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
            | Some selectedAuthorId -> authors |> List.tryFind (fun author -> author.OLID = selectedAuthorId)
            | None -> None

        Filter.FilterTab(
            key = "filter-tab-authors",
            label = translations.Product.Authors,
            iconifyIcon = Some fa6Solid.penFancy,
            items = authors,
            selectedItem = selectedAuthor,
            formatItem = _.Name,
            onSelect = (fun author -> navigateWithFilters _.ToBooksWithAuthor(Some author.OLID)),
            onReset = (fun () -> navigateWithFilters _.ToBooksWithAuthor(None))
        )

    member _.booksTags products =
        let tags =
            products
            |> List.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Tags
                | _ -> []
            )
            |> List.distinct
            |> List.sort

        Filter.FilterTab(
            key = "filter-tab-tags",
            label = translations.Product.Tags,
            iconifyIcon = Some fa6Solid.tags,
            items = tags,
            selectedItem = filters.BooksTag,
            formatItem = id,
            onSelect = (fun tag -> navigateWithFilters _.ToBooksWithTag(Some tag)),
            onReset = (fun () -> navigateWithFilters _.ToBooksWithTag(None))
        )

    member _.provider (provider: Provider) (selectedProvider: Provider option) (selectProvider: Provider -> unit) text iconifyIcon page =
        Daisy.tab [
            prop.key $"tab-provider-%s{reactKeyOf provider}"
            prop.className "gap-2"
            prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
            if selectedProvider = Some provider then
                tab.active
            else
                prop.onClick (fun _ ->
                    selectProvider provider
                    Router.navigatePage page
                )
        ]

    // TODO RDE: add checkbox Highlight search terms
    member _.search =
        // TODO RDE: fix bug that reset the selected authors when searching
        let setSearchTerm searchTerm = navigateWithFilters (fun x -> { x with SearchTerm = searchTerm })

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
            tab.provider OpenLibrary selectedProvider selectProvider translations.Home.Books fa6Solid.book (Page.ProductIndex(filters.ToBooks()))
            tab.provider FakeStore selectedProvider selectProvider translations.Home.Bazaar fa6Solid.store (Page.ProductIndex(filters.ToBazaar()))

            Daisy.divider [
                divider.horizontal
                prop.key "tabs-divider"
                prop.className "mx-1"
            ]

            match selectedProvider with
            | None -> ()
            | Some FakeStore ->
                let categories =
                    [
                        BazaarCategory.Clothing, fa6Solid.shirt
                        BazaarCategory.Electronics, fa6Solid.tv
                        BazaarCategory.Jewelry, fa6Solid.gem
                    ]
                    |> List.map (fun (cat, icon) -> cat, icon, translations.Product.StoreCategoryOf cat)
                    |> List.sortBy (fun (_, _, text) -> text)

                for cat, icon, text in categories do
                    tab.bazaarCategory cat icon text

                tab.search

            | Some OpenLibrary ->
                tab.booksAuthors products
                tab.booksTags products
                tab.search
        ]
    ]