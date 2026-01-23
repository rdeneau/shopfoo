module Shopfoo.Client.Pages.Product.Index

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Glutinum.Iconify
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Filters
open Shopfoo.Client.Pages.Product.Table
open Shopfoo.Client.Pages.Product.Filters
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type private Model = { Products: Remote<Provider * Product list> }

type private Msg =
    | ResetProvider
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

let private init (filters: Filters) =
    { Products = Remote.Empty },
    Cmd.batch [
        match filters.Provider with
        | Some provider -> Cmd.ofMsg (SelectProvider provider)
        | None -> ()
    ]

let private update fillTranslations (fullContext: FullContext) msg (model: Model) =
    match msg with
    | ResetProvider -> // ↩
        { model with Products = Remote.Empty }, Cmd.none

    | SelectProvider provider ->
        { model with Products = Remote.Loading }, // ↩
        Cmd.loadProducts (fullContext.PrepareQueryWithTranslations(provider))

    | ProductsFetched(provider, Ok(data, translations)) ->
        { model with Products = Remote.Loaded(provider, data.Products @ [ Product.notFound ]) }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | ProductsFetched(_, Error apiError) ->
        { model with Products = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

type private ProviderProps = {
    Provider: Provider
    Text: string
    Icon: IconifyIcon
    Page: Page
}

[<ReactComponent>]
let IndexView (filters: Filters, fullContext: FullContext, fillTranslations) =
    match fullContext.User with
    | UserCanNotAccess Feat.Catalog ->
        React.useEffectOnce (fun () -> Router.navigatePage (Page.CurrentNotFound()))
        Html.none
    | _ ->
        let model, dispatch = React.useElmish (init filters, update fillTranslations fullContext, [||])

        React.useEffect (fun () ->
            match model.Products with
            | Remote.Loaded(provider, _) when filters.Provider <> Some provider -> dispatch ResetProvider
            | _ -> ()
        )

        let translations = fullContext.Translations
        let selectProvider provider = dispatch (SelectProvider provider)
        let reactKeyOf x = String.toKebab $"%A{x}"

        let providerCard (props: ProviderProps) =
            let key = $"card-provider-%s{reactKeyOf props.Provider}"

            Daisy.card [
                card.border
                prop.key key
                prop.className "cursor-pointer hover:shadow-lg transition-shadow inline-block min-h-[200px] min-w-[300px] mr-2"
                prop.onClick (fun _ ->
                    selectProvider props.Provider
                    Router.navigatePage props.Page
                )
                prop.children [
                    Html.figure [
                        prop.key $"%s{key}-figure"
                        prop.className "text-8xl text-center p-8"
                        prop.children [ icon props.Icon ]
                    ]
                    Daisy.cardBody [
                        prop.key $"%s{key}-body"
                        prop.className "items-center text-center"
                        prop.children [ Daisy.cardTitle [ prop.key $"%s{key}-title"; prop.text props.Text ] ]
                    ]
                ]
            ]

        let providerCardProps provider text icon changeFilters = {
            Provider = provider
            Text = text
            Icon = icon
            Page = Page.ProductIndex(changeFilters filters)
        }

        let providerCardsProps = [
            providerCardProps OpenLibrary translations.Home.Books fa6Solid.book _.ToBooks()
            providerCardProps FakeStore translations.Home.Bazaar fa6Solid.store _.ToBazaar()
        ]

        Html.section [
            prop.key "products-page"
            prop.children [
                match model.Products with
                | Remote.Empty ->
                    for props in providerCardsProps |> List.sortBy _.Text do
                        providerCard props

                | Remote.Loading -> // ↩
                    Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]

                | Remote.LoadError apiError -> // ↩
                    Alert.apiError "products-load-error" apiError fullContext.User

                | Remote.Loaded(provider, products) ->
                    IndexFilterBar "products-filter-bar" filters products (Some provider) selectProvider translations
                    IndexTable "products-table" filters products provider translations
            ]
        ]