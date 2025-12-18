module Shopfoo.Client.Pages.Product.Index

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type private Msg = // ↩
    | ProductsFetched of ApiResult<GetProductsResponse * Translations>

type private Model = { Products: Remote<Product list> }

[<RequireQualifiedAccess>]
module private Product =
    let notFound: Product = {
        SKU = SKU "99999"
        Name = "❗"
        Description = "Fake product to demo how the page handles a product not found"
        ImageUrl = ""
    }

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.GetProducts request
            Error = Error >> ProductsFetched
            Success = Ok >> ProductsFetched
        }

let private init (fullContext: FullContext) =
    { Products = Remote.Loading }, // ↩
    Cmd.loadProducts (fullContext.PrepareQueryWithTranslations())

let private update fillTranslations msg (model: Model) =
    match msg with
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
        let model, _ = React.useElmish (init fullContext, update fillTranslations, [||])
        let translations = fullContext.Translations

        Html.section [
            prop.key "products-page"
            prop.children [
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
                                            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail product.SKU.Value))
                                            prop.children [
                                                Html.td [ prop.key $"product-%i{i}-num"; prop.text (i + 1) ]
                                                Html.td [ prop.key $"product-%i{i}-name"; prop.text product.Name ]
                                                Html.td [ prop.key $"product-%i{i}-desc"; prop.text product.Description ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
            ]
        ]