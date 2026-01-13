module Shopfoo.Client.Pages.Product.Index

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Pages.Product.Columns
open Shopfoo.Client.Pages.Product.Filters
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type private Model = { Products: Remote<Provider * Product list> }

type private Msg =
    | SelectProvider of Provider
    | ProductsFetched of Provider * ApiResult<GetProductsResponse * Translations>

[<RequireQualifiedAccess>]
module private Product =
    let private fakeBook: Book = {
        ISBN = ISBN "0"
        Subtitle = ""
        Authors = [ { OLID = OLID ""; Name = "Fake" } ]
        Tags = []
    }

    let notFound: Product = {
        SKU = fakeBook.ISBN.AsSKU
        Title = "❗ Fake"
        Description = "Fake book to demo how the page handles a product not found"
        Category = Category.Books fakeBook
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

let private init (filters: FiltersModel) =
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

[<ReactComponent>]
let IndexView (filtersModel: FiltersModel, fullContext: FullContext, fillTranslations) =
    match fullContext.User with
    | UserCanNotAccess Feat.Catalog ->
        React.useEffectOnce (fun () -> Router.navigatePage (Page.CurrentNotFound()))
        Html.none
    | _ ->
        let model, dispatch =
            React.useElmish (init filtersModel, update fillTranslations fullContext, [||])

        let translations = fullContext.Translations

        let filters = Filters(filtersModel, translations)

        let products, selectedProvider =
            match model.Products with
            | Remote.Loaded(provider, products) -> products, Some provider
            | _ -> [], None

        let selectProvider provider = dispatch (SelectProvider provider)

        Html.section [
            prop.key "products-page"
            prop.children [
                filters.tabs products selectedProvider selectProvider

                match model.Products with
                | Remote.Empty -> ()
                | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]
                | Remote.LoadError apiError -> Alert.apiError "products-load-error" apiError fullContext.User
                | Remote.Loaded(provider, products) ->
                    let filteredProducts = filters.apply products provider
                    let columns = Columns(filters, provider, translations)

                    Daisy.table [
                        prop.key "product-table"
                        prop.className "table-pin-rows w-full border-collapse"
                        prop.children [
                            Html.thead [
                                prop.key "product-thead"
                                prop.child (
                                    Html.tr [
                                        for colDef in columnDefinitionsFor provider do
                                            columns.th colDef
                                    ]
                                )
                            ]
                            Html.tbody [
                                prop.key "product-table-tbody"
                                prop.children [
                                    for i, product in filteredProducts do
                                        columns.tr i product
                                ]
                            ]
                        ]
                    ]
            ]
        ]