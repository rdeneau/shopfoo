module Shopfoo.Client.Pages.Product.Index

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared
open Shopfoo.Shared.Remoting

type private Model = { Products: Remote<Product list> }

type private Msg = // ↩
    | ProductsFetched of ApiResult<GetProductsResponse * Translations>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        cmder.ofApiCall {
            Call = fun api -> api.Product.GetProducts request
            Feat = Feat.Home
            Error = Error >> ProductsFetched
            Success = Ok >> ProductsFetched
        }

let private init (fullContext: FullContext) =
    { Products = Remote.Loading }, // ↩
    Cmd.loadProducts (fullContext.PrepareQueryWithTranslations())

let private update (fullContext: ReactState<FullContext>) (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | Msg.ProductsFetched(Ok(data, translations)) ->
        { model with Products = Remote.Loaded data.Products }, // ↩
        Cmd.ofEffect (fun _ -> fullContext.Update _.FillTranslations(translations))

    | Msg.ProductsFetched(Error apiError) ->
        { model with Products = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fullContext.Update _.FillTranslations(apiError.Translations))

[<ReactComponent>]
let IndexView (fullContext: ReactState<FullContext>) =
    let translations = fullContext.Current.Translations

    let model, _ = React.useElmish (init fullContext.Current, update fullContext, [||])

    Html.section [
        prop.key "products-page"
        prop.children [
            Daisy.breadcrumbs [
                prop.key "products-title"
                prop.child (Html.ul [ Html.li [ prop.key "products-title-text"; prop.text translations.Home.Products ] ])
            ]

            match model.Products with
            | Remote.Empty -> ()
            | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]
            | Remote.LoadError apiError ->
                Daisy.alert [
                    alert.error
                    prop.key "products-load-error"
                    prop.text apiError.ErrorMessage // TODO: [Admin] display error detail to admin
                ]

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