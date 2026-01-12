module Shopfoo.Client.Pages.Product.Index

open System
open System.Text.RegularExpressions
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

// TODO RDE: add sorting options (by name, by author, etc.)
type Filters = {
    Provider: Provider option
    BooksAuthorId: OLID option
    StoreCategory: StoreCategory option
    SearchTerm: string option
}

module Filters =
    let none: Filters = {
        Provider = None
        BooksAuthorId = None
        StoreCategory = None
        SearchTerm = None
    }

    let bazaar storeCategory searchTerm : Filters = {
        none with
            Provider = Some FakeStore
            StoreCategory = storeCategory
            SearchTerm = searchTerm
    }

    let books authorId searchTerm : Filters = {
        none with
            Provider = Some OpenLibrary
            BooksAuthorId = authorId
            SearchTerm = searchTerm
    }

    let ofPage page : Filters =
        match page with
        | Page.ProductBazaar(storeCategory, searchTerm) -> bazaar storeCategory searchTerm
        | Page.ProductBooks(authorId, searchTerm) -> books authorId searchTerm
        | _ -> none

type private Model = { Products: Remote<Provider * Product list> }

type private Msg =
    | SelectProvider of Provider
    | ProductsFetched of Provider * ApiResult<GetProductsResponse * Translations>

[<RequireQualifiedAccess>]
module private Product =
    let private bookEmpty: Book = {
        ISBN = ISBN "0"
        Subtitle = ""
        Authors = []
        Tags = []
    }

    let notFound: Product = {
        SKU = bookEmpty.ISBN.AsSKU
        Title = "❗"
        Description = "Fake book to demo how the page handles a product not found"
        Category = Category.Books bookEmpty
        ImageUrl = ImageUrl.None
    }

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        let provider = request.Body.Query

        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.GetProducts request
            Error = fun err -> ProductsFetched(provider, Error err)
            Success = fun data -> ProductsFetched(provider, Ok data)
        }

let private init (filters: Filters) =
    { Products = Remote.Empty },
    Cmd.batch [
        match filters.Provider with
        | Some provider -> Cmd.ofMsg (SelectProvider provider)
        | None -> ()
    ]

let private update fillTranslations (fullContext: FullContext) msg (model: Model) =
    match msg with
    | SelectProvider provider ->
        { model with Products = Remote.Loading }, // ↩
        Cmd.loadProducts (fullContext.PrepareQueryWithTranslations(provider))

    | ProductsFetched(provider, Ok(data, translations)) ->
        { model with Products = Remote.Loaded(provider, data.Products @ [ Product.notFound ]) }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | ProductsFetched(_, Error apiError) ->
        { model with Products = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

let private highlight (term: string option) (fullText: string) (key: string) = [
    match term with
    | None
    | Some String.NullOrWhiteSpace ->
        // No highlighting
        Html.text fullText

    | Some term ->
        // Highlight occurrences of 'term' in 'fullText' (case-insensitive)

        // If fullText = "Clean Code" and term = "code" then
        // - parts = [| "Clean "; "" |]
        // - matches = [| "Code" |]
        // Result: <span>Clean </span><mark class="...">Code</mark>
        let pattern = Regex.Escape(term)
        let options = RegexOptions.IgnoreCase ||| RegexOptions.Multiline
        let parts = Regex.Split(fullText, pattern, options)
        let matches = Regex.Matches(fullText, pattern, options) |> Seq.toArray

        for i in 0 .. parts.Length - 1 do
            // Regular text
            Html.text parts[i]

            // Matched text (highlighted)
            if i < matches.Length then
                Html.mark [
                    prop.key $"%s{key}-match-%i{i}"
                    prop.className "bg-yellow-200 text-black rounded-sm"
                    prop.text matches[i].Value
                ]
]

[<RequireQualifiedAccess>]
type private Col =
    | Num // TODO RDE: Col.Num -> Col.SKU
    | Name
    | Authors
    // TODO RDE: Col.Tags
    | Category
    | Description

[<ReactComponent>]
let IndexView (filters: Filters, fullContext: FullContext, fillTranslations) =
    match fullContext.User with
    | UserCanNotAccess Feat.Catalog ->
        React.useEffectOnce (fun () -> Router.navigatePage (Page.CurrentNotFound()))
        Html.none
    | _ ->
        let model, dispatch =
            React.useElmish (init filters, update fillTranslations fullContext, [||])

        let translations = fullContext.Translations

        Html.section [
            prop.key "products-page"
            prop.children [
                let providerTab provider text iconifyIcon page =
                    let key = String.toKebab $"%A{provider}"

                    Daisy.tab [
                        match model.Products with
                        | Remote.Loaded(selectedProvider, _) when selectedProvider = provider -> tab.active
                        | _ -> ()
                        prop.key $"tab-provider-%s{key}"
                        prop.className "gap-2"
                        prop.onClick (fun _ ->
                            dispatch (SelectProvider provider)
                            Router.navigatePage page
                        )
                        prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
                    ]

                let storeCategoryTab storeCategory text iconifyIcon page =
                    let key = String.toKebab $"%A{storeCategory}"

                    Daisy.tab [
                        if filters.StoreCategory = Some storeCategory then
                            tab.active
                        prop.key $"tab-store-category-%s{key}"
                        prop.className "gap-2"
                        prop.onClick (fun _ -> Router.navigatePage page)
                        prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
                    ]

                Daisy.tabs [
                    tabs.border
                    prop.key "tabs-providers"
                    prop.className "pb-2 border-b border-gray-200"
                    prop.children [
                        providerTab OpenLibrary translations.Home.Books fa6Solid.book (Page.ProductBooks(filters.BooksAuthorId, filters.SearchTerm))
                        providerTab FakeStore translations.Home.Bazaar fa6Solid.store (Page.ProductBazaar(filters.StoreCategory, filters.SearchTerm))

                        Daisy.divider [
                            divider.horizontal
                            prop.key "tabs-divider"
                            prop.className "mx-1"
                        ]

                        match model.Products with
                        | Remote.Loaded(FakeStore, _) ->
                            storeCategoryTab
                                StoreCategory.Clothing
                                translations.Product.StoreCategory.Clothing
                                fa6Solid.shirt
                                (Page.ProductBazaar(Some StoreCategory.Clothing, filters.SearchTerm))

                            storeCategoryTab
                                StoreCategory.Electronics
                                translations.Product.StoreCategory.Electronics
                                fa6Solid.tv
                                (Page.ProductBazaar(Some StoreCategory.Electronics, filters.SearchTerm))

                            storeCategoryTab
                                StoreCategory.Jewelry
                                translations.Product.StoreCategory.Jewelry
                                fa6Solid.gem
                                (Page.ProductBazaar(Some StoreCategory.Jewelry, filters.SearchTerm))

                        | Remote.Loaded(OpenLibrary, products) ->
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
                                Router.navigatePage (Page.ProductBooks(authorId, searchTerm = filters.SearchTerm))

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

                            // TODO RDE: add tags filter tab too

                        | _ -> ()

                        match model.Products with
                        | Remote.Loaded(provider, _) ->
                            let setSearchTerm searchTerm =
                                match provider with
                                | OpenLibrary -> Page.ProductBooks(filters.BooksAuthorId, searchTerm)
                                | FakeStore -> Page.ProductBazaar(filters.StoreCategory, searchTerm)
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
                        | _ -> ()
                    ]
                ]

                match model.Products with
                | Remote.Empty -> ()
                | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]
                | Remote.LoadError apiError -> Alert.apiError "products-load-error" apiError fullContext.User
                | Remote.Loaded(provider, products) ->
                    let columns = [
                        Col.Num
                        if provider = FakeStore then
                            Col.Category
                        Col.Name
                        if provider = OpenLibrary then
                            Col.Authors
                        Col.Description
                    ]

                    let filteredProducts =
                        products
                        |> List.filter (fun product ->
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

                            isSearchTermMatched && isAuthorMatched && isStoreCategoryMatched
                        )

                    let highlight = highlight filters.SearchTerm

                    Daisy.table [
                        prop.key "product-table"
                        prop.className "table-pin-rows w-full border-collapse"
                        prop.children [
                            Html.thead [
                                prop.key "product-thead"
                                prop.child (
                                    Html.tr [
                                        for col in columns do
                                            match col with
                                            | Col.Num -> Html.th [ prop.key "product-th-num"; prop.text " " ]
                                            | Col.Name -> Html.th [ prop.key "product-th-name"; prop.text translations.Product.Name ]
                                            | Col.Authors -> Html.th [ prop.key "product-th-authors"; prop.text translations.Product.Authors ]
                                            | Col.Category -> Html.th [ prop.key "product-th-category"; prop.text translations.Product.Category ]
                                            | Col.Description -> Html.th [ prop.key "product-th-desc"; prop.text translations.Product.Description ]
                                    ]
                                )
                            ]
                            Html.tbody [
                                prop.key "product-table-tbody"
                                prop.children [
                                    for i, product in List.indexed filteredProducts do
                                        let productKey = $"product-%i{i}"

                                        let book =
                                            match provider, product.Category with
                                            | OpenLibrary, Category.Books book ->
                                                Some {|
                                                    Subtitle = book.Subtitle // ↩
                                                    Authors = book.Authors |> List.map _.Name |> String.concat ", "
                                                |}
                                            | _ -> None

                                        let storeProduct =
                                            match provider, product.Category with
                                            | FakeStore, Category.Store storeProduct -> Some storeProduct
                                            | _ -> None

                                        Html.tr [
                                            prop.key productKey
                                            prop.className "group hover:bg-accent hover:fg-accent hover:cursor-pointer"
                                            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail product.SKU))
                                            prop.children [
                                                for col in columns do
                                                    match col with
                                                    | Col.Num ->
                                                        Html.td [ // ↩
                                                            prop.key $"%s{productKey}-num"
                                                            prop.text (i + 1)
                                                        ]
                                                    | Col.Name ->
                                                        Html.td [
                                                            prop.key $"%s{productKey}-name"
                                                            prop.children [
                                                                Html.div [
                                                                    prop.key $"%s{productKey}-name-content"
                                                                    prop.className "w-60 line-clamp-2 group-hover:line-clamp-3"

                                                                    prop.children [
                                                                        Html.div [
                                                                            prop.key $"%s{productKey}-title"
                                                                            prop.className "group-hover:inline"
                                                                            prop.children (highlight product.Title $"%s{productKey}-title-text")
                                                                        ]

                                                                        match book with
                                                                        | Some book when not (String.IsNullOrWhiteSpace book.Subtitle) ->
                                                                            Html.span [
                                                                                prop.key $"%s{productKey}-sep"
                                                                                prop.className "hidden group-hover:inline"
                                                                                prop.text ":"
                                                                            ]

                                                                            Html.div [
                                                                                prop.key $"%s{productKey}-subtitle"
                                                                                prop.className "italic ml-1 group-hover:inline"
                                                                                prop.children (highlight book.Subtitle $"%s{productKey}-subtitle-text")
                                                                            ]
                                                                        | _ -> ()
                                                                    ]
                                                                ]
                                                            ]
                                                        ]

                                                    | Col.Authors ->
                                                        Html.td [
                                                            prop.key $"%s{productKey}-authors"
                                                            prop.children [
                                                                Html.div [
                                                                    prop.key $"%s{productKey}-authors-content"
                                                                    prop.className "w-40 line-clamp-2 group-hover:line-clamp-3"
                                                                    match book with
                                                                    | Some book -> prop.text book.Authors
                                                                    | None -> prop.text " "
                                                                ]
                                                            ]
                                                        ]

                                                    | Col.Category ->
                                                        Html.td [
                                                            prop.key $"%s{productKey}-category"
                                                            prop.className "w-30"
                                                            match storeProduct with
                                                            | Some { Category = StoreCategory.Clothing } ->
                                                                prop.text translations.Product.StoreCategory.Clothing
                                                            | Some { Category = StoreCategory.Electronics } ->
                                                                prop.text translations.Product.StoreCategory.Electronics
                                                            | Some { Category = StoreCategory.Jewelry } ->
                                                                prop.text translations.Product.StoreCategory.Jewelry
                                                            | None -> ()
                                                        ]

                                                    | Col.Description ->
                                                        Html.td [
                                                            prop.key $"%s{productKey}-desc"
                                                            prop.children [
                                                                Html.div [
                                                                    prop.key $"%s{productKey}-desc-content"
                                                                    prop.className "line-clamp-2 group-hover:line-clamp-3"
                                                                    prop.children (highlight product.Description $"%s{productKey}-desc-text")
                                                                ]
                                                            ]
                                                        ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
            ]
        ]