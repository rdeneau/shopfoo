module Shopfoo.Client.Pages.Product.Index

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
    | ProductsFetched of ApiResult<GetProductsResponse * Translations>

type private Model = { Provider: Provider option; Products: Remote<Product list> }

[<RequireQualifiedAccess>]
module private Product =
    let private fs0 = { // ↩
        FSID = FSID 0
        Category = StoreCategory.Jewelry
    }

    let notFound: Product = {
        SKU = fs0.FSID.AsSKU
        Title = "❗"
        Description = "Fake product to demo how the page handles a product not found"
        Category = Category.Store fs0
        ImageUrl = ImageUrl.None
    }

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Catalog.GetProducts request
            Error = Error >> ProductsFetched
            Success = Ok >> ProductsFetched
        }

let private init () =
    { Provider = None; Products = Remote.Empty }, Cmd.none

let private update fillTranslations (fullContext: FullContext) msg (model: Model) =
    match msg with
    | SelectProvider provider ->
        { model with Provider = Some provider; Products = Remote.Loading }, // ↩
        Cmd.loadProducts (fullContext.PrepareQueryWithTranslations(provider))

    | Msg.ProductsFetched(Ok(data, translations)) ->
        { model with Products = Remote.Loaded(data.Products @ [ Product.notFound ]) }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | Msg.ProductsFetched(Error apiError) ->
        { model with Products = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

[<ReactComponent>]
let IndexView (fullContext: FullContext, fillTranslations) =
    match fullContext.User with
    | UserCanNotAccess Feat.Catalog ->
        React.useEffectOnce (fun () -> Router.navigatePage (Page.CurrentNotFound()))
        Html.none
    | _ ->
        let model, dispatch =
            React.useElmish (init (), update fillTranslations fullContext, [||])

        let translations = fullContext.Translations

        Html.section [
            prop.key "products-page"
            prop.children [
                let providerTab provider text iconifyIcon =
                    let key = String.toKebab $"%A{provider}"
                    Daisy.tab [
                        match model.Provider with
                        | Some selectedProvider when selectedProvider = provider -> tab.active
                        | _ -> ()
                        prop.key $"tab-%s{key}"
                        prop.className "gap-2"
                        prop.onClick (fun _ -> dispatch (SelectProvider provider))
                        prop.children [ icon iconifyIcon; Html.text $"%s{text}" ]
                    ]

                Daisy.tabs [
                    tabs.border
                    prop.key "tabs-providers"
                    prop.children [
                        providerTab OpenLibrary "Open Library" fa6Solid.book
                        providerTab FakeStore "Fake Store" fa6Solid.store
                    ]
                ]

                match model.Products with
                | Remote.Empty -> ()
                | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]
                | Remote.LoadError apiError -> Alert.apiError "products-load-error" apiError fullContext.User
                | Remote.Loaded products ->
                    Daisy.table [
                        prop.key "products-table"
                        prop.className "table-pin-rows w-full"
                        prop.children [
                            Html.thead [
                                prop.key "products-table-thead"
                                prop.child (
                                    Html.tr [
                                        Html.th [ prop.key "products-table-header-num"; prop.text " " ]
                                        Html.th [ prop.key "products-table-header-name"; prop.text translations.Product.Name ]
                                        Html.th [ prop.key "products-table-header-desc"; prop.text translations.Product.Description ]
                                    ]
                                )
                            ]
                            Html.tbody [
                                prop.key "products-table-tbody"
                                prop.children [
                                    for i, product in List.indexed products do
                                        Html.tr [
                                            prop.key $"product-%i{i}"
                                            prop.className "hover:bg-accent hover:fg-accent hover:cursor-pointer"
                                            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail product.SKU.Key))
                                            prop.children [
                                                Html.td [ prop.key $"product-%i{i}-num"; prop.text (i + 1) ]
                                                Html.td [ prop.key $"product-%i{i}-name"; prop.text product.Title ]
                                                Html.td [ prop.key $"product-%i{i}-desc"; prop.text product.Description ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
            ]
        ]