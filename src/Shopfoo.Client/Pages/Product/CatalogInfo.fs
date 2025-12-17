module Shopfoo.Client.Pages.Product.CatalogInfo

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type private Model = { Product: Remote<Product> }

type private Msg = // ↩
    | ProductDetailsFetched of ApiResult<GetProductDetailsResponse * Translations>
    | ProductChanged of Product

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

let private update fillTranslations (msg: Msg) (model: Model) =
    match msg with
    | ProductDetailsFetched(Ok(data, translations)) ->
        let product =
            match data.Product with
            | Some product -> Remote.Loaded product
            | None -> Remote.Empty

        { model with Product = product }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | ProductDetailsFetched(Error apiError) ->
        { model with Product = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

    | ProductChanged product -> { model with Product = Remote.Loaded product }, Cmd.none

[<ReactComponent>]
let CatalogInfoSection (key, fullContext, sku, fillTranslations) =
    let model, dispatch =
        React.useElmish (init fullContext sku, update fillTranslations, [||])

    let translations = fullContext.Translations

    Html.section [
        prop.key $"%s{key}-section"
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
                                            prop.onChange (fun imageUrl -> dispatch (ProductChanged { product with ImageUrl = imageUrl }))
                                            prop.value product.ImageUrl
                                        ]

                                        Daisy.fieldsetLabel [ prop.key "name-label"; prop.text translations.Product.Name ]
                                        Daisy.input [
                                            prop.key "name-input"
                                            prop.placeholder translations.Product.Name
                                            prop.className "mb-4 w-full"
                                            prop.onChange (fun name -> dispatch (ProductChanged { product with Name = name }))
                                            prop.value product.Name
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

                        Daisy.fieldsetLabel [ prop.key "description-label"; prop.text translations.Product.Description ]
                        Daisy.textarea [
                            prop.key "description-textarea"
                            prop.placeholder translations.Product.Description
                            prop.className "h-21 mb-4 w-full"
                            prop.onChange (fun description -> dispatch (ProductChanged { product with Description = description }))
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
        ]
    ]