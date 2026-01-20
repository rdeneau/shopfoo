module Shopfoo.Client.Pages.Product.Filters

open Feliz
open Feliz.DaisyUI
open Feliz.Router
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Routing
open Shopfoo.Client.Filters
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

type private Tab(filters: Filters, translations: AppTranslations) =
    let reactKeyOf x = String.toKebab $"%A{x}"
    let pageWithFilters changeFilters = Page.ProductIndex(changeFilters filters)

    member _.bazaarCategory (count: int) bazaarCategory iconifyIcon text =
        Daisy.tab [
            let key = $"tab-bazaar-category-%s{reactKeyOf bazaarCategory}"
            prop.key key

            let isActive = (filters.BazaarCategory = Some bazaarCategory)

            if isActive then
                tab.active
                yield! prop.hrefRouted (pageWithFilters _.ToBazaar())
            else
                yield! prop.hrefRouted (pageWithFilters _.ToBazaarWithCategory(bazaarCategory))

            prop.children [
                Daisy.indicator [
                    prop.key $"%s{key}-indicator"
                    prop.children [
                        Daisy.indicatorItem [
                            prop.key $"%s{key}-badge"
                            prop.className "badge badge-sm badge-primary badge-soft px-1"
                            prop.text count
                        ]
                        Html.div [
                            prop.key $"%s{key}-content"
                            prop.className "flex items-center gap-2 pr-3"
                            prop.children [
                                icon iconifyIcon
                                Html.text $"%s{text}"
                                if isActive then
                                    Html.span [
                                        prop.key $"%s{key}-tab-close-button"
                                        prop.className "btn btn-ghost btn-sm btn-circle"
                                        prop.text "✕"
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
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
            onSelect = FilterAction.NavigateToPage(fun author -> pageWithFilters _.ToBooksWithAuthor(Some author.OLID)),
            onReset = FilterAction.NavigateToPage(fun () -> pageWithFilters _.ToBooksWithAuthor(None))
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
            onSelect = FilterAction.NavigateToPage(fun tag -> pageWithFilters _.ToBooksWithTag(Some tag)),
            onReset = FilterAction.NavigateToPage(fun () -> pageWithFilters _.ToBooksWithTag(None))
        )

    member _.provider (count: int) (selectedProvider: Provider option) (selectProvider: Provider -> unit) (provider: Provider) text iconifyIcon page =
        let (PageUrl pageUrl) = page
        let key = $"tab-provider-%s{reactKeyOf provider}"

        Daisy.tab [
            prop.key key
            prop.children [
                Daisy.indicator [
                    prop.key $"%s{key}-count-indicator"
                    prop.children [
                        if selectedProvider = Some provider then
                            Daisy.indicatorItem [
                                prop.key $"%s{key}-count-badge"
                                prop.className "badge badge-sm badge-primary badge-soft px-1"
                                prop.text count
                            ]

                        Html.div [
                            prop.key $"%s{key}-count-badge-content"
                            prop.className "flex items-center gap-2 pr-3"
                            prop.children [
                                icon iconifyIcon // ↩
                                Html.text $"%s{text}"
                            ]
                        ]
                    ]
                ]
            ]
            if selectedProvider = Some provider then
                tab.active
            else
                prop.href (Router.formatPath (pageUrl.Segments, queryString = pageUrl.Query))

                prop.onClick (fun ev ->
                    selectProvider provider
                    Router.goToUrl ev
                )
        ]

    // TODO RDE: add search config: toggle Highlighting, search in titles only, etc.
    member _.search =
        let setSearchTerm searchTerm = // ↩
            pageWithFilters (fun x -> { x with SearchTerm = searchTerm }) |> Router.navigatePage

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
let IndexFilterBar
    key
    (filters: Filters)
    (products: Product list)
    (selectedProvider: Provider option)
    selectProvider
    (translations: AppTranslations)
    =
    let tab = Tab(filters, translations)

    Daisy.tabs [
        tabs.border
        prop.key $"%s{key}-content"
        prop.className "pb-2 border-b border-gray-200"
        prop.children [
            let count = products.Length

            tab.provider
                count
                selectedProvider
                selectProvider
                OpenLibrary
                translations.Home.Books
                fa6Solid.book
                (Page.ProductIndex(filters.ToBooks()))

            tab.provider
                count
                selectedProvider
                selectProvider
                FakeStore
                translations.Home.Bazaar
                fa6Solid.store
                (Page.ProductIndex(filters.ToBazaar()))

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
                    let count =
                        products
                        |> List.sumBy (fun p ->
                            match p.Category with
                            | Category.Bazaar bazaarProduct when bazaarProduct.Category = cat -> 1
                            | _ -> 0
                        )

                    tab.bazaarCategory count cat icon text

                tab.search

            | Some OpenLibrary ->
                tab.booksAuthors products
                tab.booksTags products
                tab.search
        ]
    ]