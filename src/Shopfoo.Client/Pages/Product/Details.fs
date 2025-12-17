module Shopfoo.Client.Pages.Product.Details

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

type private Model = { Product: Remote<Product> }

type private Msg = // ↩
    | ProductDetailsFetched of ApiResult<GetProductDetailsResponse * Translations>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.GetProductDetails request
            Error = Error >> ProductDetailsFetched
            Success = Ok >> ProductDetailsFetched
        }

let private init (fullContext: FullContext) sku =
    { Product = Remote.Loading }, // ↩
    Cmd.loadProducts (fullContext.PrepareQueryWithTranslations sku)

let private update fillTranslations msg (model: Model) =
    match msg with
    | Msg.ProductDetailsFetched(Ok(data, translations)) ->
        let product =
            match data.Product with
            | Some product -> Remote.Loaded product
            | None -> Remote.Empty

        { model with Product = product }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | Msg.ProductDetailsFetched(Error apiError) ->
        { model with Product = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

[<AutoOpen>]
module private Component =
    type Action = {
        Key: string
        Text: string
        OnClick: unit -> unit
    } with
        static member Emoji(emoji, key, text, onClick: unit -> unit) : Action = {
            Key = key
            Text = $"%s{emoji}  {text}"
            OnClick = onClick
        }

    type Value =
        | Natural of value: int
        | Money of value: decimal * currency: string

        static member Dollars(value) = Money(value, "$")
        static member Euros(value) = Money(value, "€")

        member this.Symbol =
            match this with
            | Value.Natural _ -> None
            | Value.Money(_, symbol) -> Some symbol

        member this.Text =
            match this with
            | Value.Natural value -> $"%i{value}"
            | Value.Money(value, _) -> $"%0.2f{value}"

type private Component =
    [<ReactComponent>]
    static member InputWithActions(key: string, value: Value, actions: Action list) =
        Html.div [
            prop.key $"{key}-div"
            prop.className "flex items-center mb-4 w-full"
            prop.children [
                Daisy.label.input [
                    prop.key $"{key}-label-input"
                    prop.className "bg-base-300 flex-1"
                    prop.children [
                        match value.Symbol with
                        | Some symbol -> Daisy.label [ prop.key $"{key}-label-symbol"; prop.text symbol ]
                        | None -> ()

                        Html.input [
                            prop.key $"{key}-input"
                            prop.className "flex-1"
                            prop.defaultValue value.Text
                            prop.readOnly true
                            prop.type' "text"
                        ]
                    ]
                ]
                Daisy.dropdown [
                    dropdown.hover
                    dropdown.end'
                    prop.key $"{key}-dropdown"
                    prop.className "ml-2"
                    prop.children [
                        Daisy.button.button [ // ↩
                            button.primary
                            button.outline
                            prop.key $"{key}-dropdown-button"
                            prop.className "p-3"
                            prop.text "⏷"
                        ]
                        Daisy.dropdownContent [
                            prop.key $"{key}-dropdown-content"
                            prop.className "p-2 shadow menu bg-base-100 rounded-box"
                            prop.tabIndex 0
                            prop.children [
                                Html.ul [
                                    prop.key $"{key}-dropdown-list"
                                    prop.children [
                                        for action in actions do
                                            Html.li [
                                                prop.key $"{key}-action--{action.Key}"
                                                prop.children [
                                                    Html.a [
                                                        prop.key $"{key}-action--{action.Key}--link"
                                                        prop.text action.Text
                                                        prop.className "whitespace-nowrap"
                                                        prop.onClick (fun _ -> action.OnClick())
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

module private Section =
    let ProductCatalogInfo (product: Product) dispatch (translations: AppTranslations) =
        // TODO: [Product] handle user claims

        Daisy.fieldset [
            prop.key "product-details-fieldset"
            prop.className "bg-base-200 border border-base-300 rounded-box p-4"
            prop.children [
                Html.legend [
                    prop.key "product-details-legend"
                    prop.className "text-sm"
                    prop.text $"🗂️ %s{translations.Product.CatalogInfo}"
                ]

                Html.div [
                    prop.key "image-grid"
                    prop.className "grid grid-cols-[1fr_max-content] gap-4 items-center"
                    prop.children [
                        Html.div [
                            prop.key "image-input-div"
                            prop.children [
                                Daisy.fieldsetLabel [ prop.key "image-label"; prop.text translations.Product.ImageUrl ]
                                Daisy.input [
                                    prop.key "image-input-column"
                                    prop.placeholder translations.Product.ImageUrl
                                    prop.className "mb-4 w-full"
                                    prop.onChange (fun (value: string) -> ()) // TODO
                                    prop.value product.ImageUrl
                                ]
                            ]
                        ]
                        Html.div [
                            prop.key "image-preview-column"
                            prop.children [
                                Html.img [
                                    prop.key "image-preview"
                                    prop.src product.ImageUrl
                                    prop.width 115
                                    prop.height 62
                                ]
                            ]
                        ]
                    ]
                ]

                Daisy.fieldsetLabel [ prop.key "name-label"; prop.text translations.Product.Name ]
                Daisy.input [
                    prop.key "name-input"
                    prop.placeholder translations.Product.Name
                    prop.className "mb-4 w-full"
                    prop.onChange (fun (value: string) -> ()) // TODO
                    prop.value product.Name
                ]

                Daisy.fieldsetLabel [ prop.key "description-label"; prop.text translations.Product.Description ]
                Daisy.textarea [
                    prop.key "description-textarea"
                    prop.placeholder translations.Product.Description
                    prop.className "h-21 mb-4 w-full"
                    prop.onChange (fun (value: string) -> ()) // TODO
                    prop.value product.Description
                ]

                Daisy.button.button [
                    button.primary
                    prop.className "justify-self-start"
                    prop.key "save-product-button"
                    prop.text translations.Product.Save
                    prop.onClick (fun _ -> ()) // TODO
                ]
            ]
        ]

    let ProductActions =
        Daisy.fieldset [
            prop.key "product-actions-fieldset"
            prop.className "bg-base-200 border border-base-300 rounded-box p-4"
            prop.children [
                Html.legend [
                    prop.key "product-actions-legend"
                    prop.className "text-sm"
                    prop.text "⚡ Actions"
                ]

                Daisy.fieldsetLabel [ prop.key "price-label"; prop.text "Price" ]
                Component.InputWithActions(
                    "price",
                    Value.Euros 85.00m,
                    [
                        Action.Emoji("↗️", "increase", "Increase Price", fun () -> ()) // TODO
                        Action.Emoji("↘️", "decrease", "Decrease Price", fun () -> ()) // TODO
                        Action.Emoji("🚫", "unavailable", "Unavailable", fun () -> ()) // TODO
                        Action.Emoji("📦", "free-shipping", "Free Shipping", fun () -> ()) // TODO
                    ]
                )

                Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text "Stock" ]
                Component.InputWithActions(
                    "stock",
                    Value.Natural 17,
                    [
                        Action.Emoji("✏️", "inventory-adjustment", "Inventory Adjustment", fun () -> ()) // TODO
                    ]
                )
            ]
        ]

[<ReactComponent>]
let DetailsView (fullContext, sku, fillTranslations) =
    let model, dispatch =
        React.useElmish (init fullContext sku, update fillTranslations, [||])

    let translations = fullContext.Translations

    Html.section [
        prop.key "product-details-page"
        prop.children [
            // TODO: [Product] handle Remote<Product> (skeleton, details...) in Section.ProductCatalogInfo
            match model.Product with
            | Remote.Empty ->
                Daisy.alert [
                    alert.error
                    prop.key "product-not-found"
                    prop.children [
                        Html.span [
                            prop.key "pnf-icon"
                            prop.text "⛓️‍💥"
                            prop.className "text-lg mr-1"
                        ]
                        Html.span [
                            prop.key "pnf-content"
                            prop.children [
                                Html.span [ prop.key "pnf-text"; prop.text (translations.Home.ErrorNotFound translations.Home.Product) ]
                                Html.code [ prop.key "pnf-sku"; prop.text $" %s{sku.Value} " ]
                            ]
                        ]
                    ]
                ]

            | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "products-skeleton" ]
            | Remote.LoadError apiError -> Alert.apiError "product-load-error" apiError fullContext.User

            | Remote.Loaded product ->
                Html.div [
                    prop.key "product-details-grid"
                    prop.className "grid grid-cols-4 gap-4"
                    prop.children [
                        Html.div [
                            prop.key "index-page-product-details"
                            prop.className "col-span-3"
                            prop.children (Section.ProductCatalogInfo product dispatch translations)
                        ]
                        Html.div [
                            prop.key "index-page-product-actions"
                            prop.className "col-span-1"
                            prop.children Section.ProductActions
                        ]
                    ]
                ]
        ]
    ]