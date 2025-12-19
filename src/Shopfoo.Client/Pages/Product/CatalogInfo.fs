module Shopfoo.Client.Pages.Product.CatalogInfo

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type private Model = { Product: Remote<Product>; SaveDate: Remote<DateTime> }

type private Msg =
    | ProductFetched of ApiResult<GetProductResponse * Translations>
    | ProductChanged of Product
    | SaveProduct of Product * ApiCall<unit>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.GetProduct request
            Error = Error >> ProductFetched
            Success = Ok >> ProductFetched
        }

    let saveProduct (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.SaveProduct request
            Error = fun err -> SaveProduct(request.Body, Done(Error err))
            Success = fun () -> SaveProduct(request.Body, Done(Ok()))
        }

let private init (fullContext: FullContext) sku =
    { Product = Remote.Loading; SaveDate = Remote.Empty }, // ↩
    Cmd.loadProducts (fullContext.PrepareQueryWithTranslations sku)

let private update fillTranslations onSaveProduct (fullContext: FullContext) (msg: Msg) (model: Model) =
    match msg with
    | ProductFetched(Ok(response, translations)) ->
        { model with Product = response.Product |> Remote.ofOption }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | ProductFetched(Error apiError) ->
        { model with Product = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

    | ProductChanged product -> // ↩
        { model with Product = Remote.Loaded product }, Cmd.none

    | SaveProduct(product, Start) ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.saveProduct (fullContext.PrepareRequest product)

    | SaveProduct(product, Done result) ->
        let saveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult

        { model with SaveDate = saveDate }, // ↩
        Cmd.ofEffect (fun _ -> onSaveProduct (product, result |> Result.tryGetError))

[<ReactComponent>]
let CatalogInfoForm key fullContext sku fillTranslations onSaveProduct =
    let model, dispatch =
        React.useElmish (init fullContext sku, update fillTranslations onSaveProduct fullContext, [||])

    let translations = fullContext.Translations
    let catalogAccess = fullContext.User.AccessTo Feat.Catalog

    let propOnChangeOrReadonly (onChange: string -> unit) = [
        match catalogAccess with
        | Some Edit -> // ↩
            prop.onChange onChange
        | _ ->
            prop.readOnly true
            prop.className "bg-base-300"
    ]

    React.fragment [
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

        | Remote.Loading -> Daisy.skeleton [ prop.className "h-64 w-full"; prop.key "products-skeleton" ]
        | Remote.LoadError apiError -> Alert.apiError "product-load-error" apiError fullContext.User

        | Remote.Loaded product ->
            Daisy.fieldset [
                prop.key $"%s{key}-fieldset"
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
                                prop.key "image-input-column"
                                prop.children [
                                    // -- Image ----
                                    Daisy.fieldset [
                                        prop.key "image-fieldset"
                                        prop.children [
                                            let props = Product.Guard.ImageUrl.props (product.ImageUrl, translations)

                                            Daisy.fieldsetLabel [
                                                prop.key "image-label"
                                                prop.children [
                                                    Html.text translations.Product.ImageUrl
                                                    Html.small [ prop.key "image-required"; yield! props.textRequired ]
                                                    Html.small [ prop.key "image-spacer"; prop.className "flex-1" ]
                                                    Html.span [ prop.key "image-char-count"; yield! props.textCharCount ]
                                                ]
                                            ]

                                            Daisy.validator.input [
                                                prop.key "image-input"
                                                prop.className "mb-4 w-full"
                                                prop.placeholder translations.Product.ImageUrl
                                                props.value
                                                yield! props.validation
                                                yield! propOnChangeOrReadonly (fun url -> dispatch (ProductChanged { product with ImageUrl = url }))
                                            ]
                                        ]
                                    ]

                                    // -- Name ----
                                    Daisy.fieldset [
                                        prop.key "name-fieldset"
                                        prop.children [
                                            let props = Product.Guard.Name.props (product.Name, translations)

                                            Daisy.fieldsetLabel [
                                                prop.key "name-label"
                                                prop.children [
                                                    Html.text translations.Product.Name
                                                    Html.small [ prop.key "name-required"; yield! props.textRequired ]
                                                    Html.small [ prop.key "name-spacer"; prop.className "flex-1" ]
                                                    Html.span [ prop.key "name-char-count"; yield! props.textCharCount ]
                                                ]
                                            ]

                                            Daisy.validator.input [
                                                prop.key "name-input"
                                                prop.className "mb-4 w-full"
                                                prop.placeholder translations.Product.Name
                                                props.value
                                                yield! props.validation
                                                yield! propOnChangeOrReadonly (fun name -> dispatch (ProductChanged { product with Name = name }))
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // -- Preview ----
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

                    // -- Description ----
                    let props = Product.Guard.Description.props (product.Description, translations)

                    Daisy.fieldsetLabel [
                        prop.key "description-label"
                        prop.children [
                            Html.text translations.Product.Description
                            Html.small [ prop.key "description-required"; yield! props.textRequired ]
                            Html.small [ prop.key "description-spacer"; prop.className "flex-1" ]
                            Html.span [ prop.key "description-char-count"; yield! props.textCharCount ]
                        ]
                    ]

                    Daisy.textarea [
                        prop.key "description-textarea"
                        prop.className "validator h-21 mb-4 w-full"
                        prop.placeholder translations.Product.Description
                        props.value
                        yield! props.validation
                        yield! propOnChangeOrReadonly (fun description -> dispatch (ProductChanged { product with Description = description }))
                    ]

                    // -- Save ----
                    match catalogAccess with
                    | None
                    | Some View -> ()
                    | Some Edit ->
                        let productSku = $"%s{translations.Home.Product} %s{sku.Value}"

                        Buttons.SaveButton(
                            key = "save-product",
                            label = translations.Home.Save,
                            tooltipOk = translations.Home.SaveOk productSku,
                            tooltipError = (fun err -> translations.Home.SaveError(productSku, err.ErrorMessage)),
                            tooltipProps = [ tooltip.right ],
                            saveDate = model.SaveDate,
                            disabled = false,
                            onClick = (fun () -> dispatch (SaveProduct(product, Start)))
                        )
                ]
            ]
    ]