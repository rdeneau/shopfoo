module Shopfoo.Client.Pages.Product.Index

open System
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

type private Msg =
    | SelectProvider of Provider
    | ProductsFetched of Provider * ApiResult<GetProductsResponse * Translations>

type private Model = { Products: Remote<Provider * Product list> }

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

let private init categoryKey =
    { Products = Remote.Empty },
    Cmd.batch [
        match (categoryKey |> Option.bind Provider.FromCategoryKey) with
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

[<RequireQualifiedAccess>]
type private Col =
    | Num
    | Name
    | Authors
    | Category
    | Description

[<ReactComponent>]
let IndexView (categoryKey: string option, fullContext: FullContext, fillTranslations) =
    match fullContext.User with
    | UserCanNotAccess Feat.Catalog ->
        React.useEffectOnce (fun () -> Router.navigatePage (Page.CurrentNotFound()))
        Html.none
    | _ ->
        let model, dispatch =
            React.useElmish (init categoryKey, update fillTranslations fullContext, [||])

        let translations = fullContext.Translations

        Html.section [
            prop.key "products-page"
            prop.children [
                let providerTab provider text iconifyIcon =
                    let key = String.toKebab $"%A{provider}"

                    Daisy.tab [
                        match model.Products with
                        | Remote.Loaded(selectedProvider, _) when selectedProvider = provider -> tab.active
                        | _ -> ()
                        prop.key $"tab-%s{key}"
                        prop.className "gap-2"
                        prop.onClick (fun _ ->
                            dispatch (SelectProvider provider)
                            Router.navigatePage (Page.ProductIndex(Some provider.CategoryKey))
                        )
                        prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
                    ]

                Daisy.tabs [
                    tabs.border
                    prop.key "tabs-providers"
                    prop.className "mb-2"
                    prop.children [ // ↩
                        providerTab OpenLibrary translations.Home.Books fa6Solid.book
                        providerTab FakeStore translations.Home.Bazaar fa6Solid.store
                    ]
                ]

                match model.Products with
                | Remote.Empty -> ()
                | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]
                | Remote.LoadError apiError -> Alert.apiError "products-load-error" apiError fullContext.User
                | Remote.Loaded(provider, products) ->
                    let columns = [
                        Col.Num
                        Col.Name
                        match provider with
                        | FakeStore -> Col.Category
                        | OpenLibrary -> Col.Authors
                        Col.Description
                    ]

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
                                    for i, product in List.indexed products do
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
                                            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail product.SKU.Key))
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
                                                                            prop.text product.Title
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
                                                                                prop.text book.Subtitle
                                                                                prop.className "italic ml-1 group-hover:inline"
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
                                                            | Some { Category = StoreCategory.Clothing} -> prop.text translations.Product.StoreCategory.Clothing
                                                            | Some { Category = StoreCategory.Electronics} -> prop.text translations.Product.StoreCategory.Electronics
                                                            | Some { Category = StoreCategory.Jewelry} -> prop.text translations.Product.StoreCategory.Jewelry
                                                            | None -> ()
                                                        ]
                                                    | Col.Description ->
                                                        Html.td [
                                                            prop.key $"%s{productKey}-desc"
                                                            prop.children [
                                                                Html.div [
                                                                    prop.key $"%s{productKey}-desc-content"
                                                                    prop.text product.Description
                                                                    prop.className "line-clamp-2 group-hover:line-clamp-3"
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