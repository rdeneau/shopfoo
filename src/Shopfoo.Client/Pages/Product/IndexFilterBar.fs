module Shopfoo.Client.Pages.Product.Filters

open Feliz
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Feliz.Router
open Glutinum.Iconify
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Routing
open Shopfoo.Client.Filters
open Shopfoo.Client.Search
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

type private BazaarTabProps = {
    Category: BazaarCategory
    Icon: IconifyIcon
    Text: string
    ProductCount: int
}

type private ProviderTabProps = {
    Provider: Provider
    SelectedProvider: Provider option
    SelectProvider: Provider -> unit
    Text: string
    Icon: IconifyIcon
    ProductCount: int
    TargetPage: Page
}

type private Tab(filters: Filters, translations: AppTranslations) =
    let reactKeyOf x = String.toKebab $"%A{x}"
    let pageWithFilters changeFilters = Page.ProductIndex(changeFilters filters)

    member _.divider key =
        Daisy.divider [
            divider.horizontal
            prop.key $"%s{key}"
            prop.className "mx-1"
        ]

    member _.bazaarCategory(props: BazaarTabProps) =
        Daisy.tab [
            let key = $"tab-bazaar-category-%s{reactKeyOf props.Category}"
            prop.key key

            let isActive = (filters.BazaarCategory = Some props.Category)

            if isActive then
                tab.active
                yield! prop.hrefRouted (pageWithFilters _.ToBazaar())
            else
                yield! prop.hrefRouted (pageWithFilters _.ToBazaarWithCategory(props.Category))

            prop.children [
                Daisy.indicator [
                    prop.key $"%s{key}-indicator"
                    prop.children [
                        Daisy.indicatorItem [
                            prop.key $"%s{key}-badge"
                            prop.className "badge badge-sm badge-primary badge-soft px-1"
                            prop.text props.ProductCount
                        ]
                        Html.div [
                            prop.key $"%s{key}-content"
                            prop.className "flex items-center gap-2 pr-3"
                            prop.children [
                                icon props.Icon
                                Html.text $"%s{props.Text}"
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

    member _.booksAuthors(products: Product list) =
        let authors =
            products
            |> Seq.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Authors
                | _ -> Set.empty
            )
            |> Seq.distinct
            |> Seq.sortBy _.Name
            |> Seq.toList

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

    member _.booksTags(products: Product list) =
        let tags =
            products
            |> Seq.collect (fun p ->
                match p.Category with
                | Category.Books book -> book.Tags
                | _ -> Set.empty
            )
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toList

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

    member _.provider(props: ProviderTabProps) =
        let (PageUrl pageUrl) = props.TargetPage
        let key = $"tab-provider-%s{reactKeyOf props.Provider}"

        Daisy.tab [
            prop.key key
            prop.children [
                Daisy.indicator [
                    prop.key $"%s{key}-count-indicator"
                    prop.children [
                        if props.SelectedProvider = Some props.Provider then
                            Daisy.indicatorItem [
                                prop.key $"%s{key}-count-badge"
                                prop.className "badge badge-sm badge-primary badge-soft px-1"
                                prop.text props.ProductCount
                            ]

                        Html.div [
                            prop.key $"%s{key}-count-badge-content"
                            prop.className "flex items-center gap-2 pr-3"
                            prop.children [
                                icon props.Icon // ↩
                                Html.text $"%s{props.Text}"
                            ]
                        ]
                    ]
                ]
            ]
            if props.SelectedProvider = Some props.Provider then
                tab.active
            else
                prop.href (Router.formatPath (pageUrl.Segments, queryString = pageUrl.Query))

                prop.onClick (fun ev ->
                    props.SelectProvider props.Provider
                    Router.goToUrl ev
                )
        ]

    member private _.toggle(key, text, isChecked, onCheckedChange) =
        Daisy.fieldset [
            prop.key $"%s{key}-fieldset"
            prop.children [
                Daisy.fieldsetLabel [
                    prop.key $"%s{key}-label"
                    prop.className "text-sm whitespace-nowrap"
                    prop.children [
                        Daisy.toggle [
                            prop.key $"%s{key}-toggle"
                            prop.isChecked isChecked
                            prop.onCheckedChange onCheckedChange
                        ]
                        Html.text $"%s{text}"
                    ]
                ]
            ]
        ]

    member this.search =
        let setCaseSensitive b =
            pageWithFilters (fun filters -> { filters with Filters.Search.CaseMatching = CaseSensitiveIf b })
            |> Router.navigatePage

        let setHighlighting b =
            pageWithFilters (fun filters -> { filters with Filters.Search.Highlighting = Highlighting.ActiveIf b })
            |> Router.navigatePage

        let setSearchTerm searchTerm =
            pageWithFilters (fun filters -> { filters with Filters.Search.Term = searchTerm })
            |> Router.navigatePage

        Html.div [
            prop.key "search-bar"
            prop.className "flex items-center gap-3 ml-auto"
            prop.children [
                Daisy.label.input [
                    prop.key "search-box"
                    prop.className "ml-2"
                    prop.children [
                        icon fa6Solid.magnifyingGlass
                        Html.input [
                            prop.key "search-input"
                            prop.type' "text"
                            prop.placeholder translations.Home.Search
                            match filters.Search.Term with
                            | Some value -> prop.value value
                            | None -> prop.value ""
                            prop.onChange (fun searchTerm ->
                                match searchTerm |> String.trimWhiteSpace with
                                | "" -> setSearchTerm None
                                | s -> setSearchTerm (Some s)
                            )
                        ]
                        if filters.Search.Term.IsSome then
                            Daisy.button.button [
                                button.ghost ++ button.circle ++ button.sm
                                prop.key "search-tab-close-button"
                                prop.className "mr-[-8px]"
                                prop.onClick (fun _ -> setSearchTerm None)
                                prop.text "✕"
                            ]
                    ]
                ]
                this.toggle (
                    key = "matchCase",
                    text = translations.Product.MatchCase,
                    isChecked = (filters.Search.CaseMatching = CaseSensitive),
                    onCheckedChange = setCaseSensitive
                )
                this.toggle (
                    key = "highlightMatches",
                    text = translations.Product.HighlightMatches,
                    isChecked = (filters.Search.Highlighting = Highlighting.Active),
                    onCheckedChange = setHighlighting
                )
            ]
        ]

[<ReactComponent>]
let IndexFilterBar key (filters: Filters) (products: Product list) selectedProvider selectProvider (translations: AppTranslations) =
    let tab = Tab(filters, translations)

    let productCountByCategory =
        products
        |> List.countBy (fun p ->
            match p.Category with
            | Category.Bazaar bazaarProduct -> Some bazaarProduct.Category
            | _ -> None
        )
        |> Map.ofList

    let bazaarTab category icon : BazaarTabProps = {
        Category = category
        Icon = icon
        Text = translations.Product.StoreCategoryOf category
        ProductCount = productCountByCategory[Some category]
    }

    let bazaarTabs () = [
        bazaarTab BazaarCategory.Jewelry fa6Solid.gem
        bazaarTab BazaarCategory.Clothing fa6Solid.shirt
        bazaarTab BazaarCategory.Electronics fa6Solid.tv
    ]

    let providerTab provider text icon changeFilters : ProviderTabProps = {
        Provider = provider
        SelectedProvider = selectedProvider
        SelectProvider = selectProvider
        Text = text
        Icon = icon
        ProductCount = products.Length
        TargetPage = Page.ProductIndex(changeFilters filters)
    }

    let providerTabs () = [
        providerTab OpenLibrary translations.Home.Books fa6Solid.book _.ToBooks()
        providerTab FakeStore translations.Home.Bazaar fa6Solid.store _.ToBazaar()
    ]

    Daisy.tabs [
        tabs.border
        prop.key $"%s{key}-content"
        prop.className "pb-2 border-b border-gray-200"
        prop.children [
            for props in providerTabs () |> List.sortBy _.Text do
                tab.provider props

            tab.divider "provider-divider"

            match selectedProvider with
            | None -> ()
            | Some FakeStore ->
                for props in bazaarTabs () |> List.sortBy _.Text do
                    tab.bazaarCategory props

                tab.search

            | Some OpenLibrary ->
                tab.booksAuthors products
                tab.booksTags products
                tab.search
        ]
    ]