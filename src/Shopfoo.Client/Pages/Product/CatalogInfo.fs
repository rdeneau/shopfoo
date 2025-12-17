module Shopfoo.Client.Pages.Product.CatalogInfo

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type private Model = { Product: Remote<Product>; SaveDate: Remote<DateTime> }

type private Msg =
    | ProductDetailsFetched of ApiResult<GetProductResponse * Translations>
    | ProductChanged of Product
    | SaveProduct of Product * ApiCall<unit>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadProducts (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.GetProduct request
            Error = Error >> ProductDetailsFetched
            Success = Ok >> ProductDetailsFetched
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

    | ProductChanged product -> // ↩
        { model with Product = Remote.Loaded product }, Cmd.none

    | SaveProduct(product, Start) ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.saveProduct (fullContext.PrepareRequest product)

    | SaveProduct(product, Done result) ->
        {
            model with
                SaveDate =
                    result // ↩
                    |> Result.map (fun () -> DateTime.Now)
                    |> Remote.ofResult
        },
        Cmd.ofEffect (fun _ ->
            let optionalError =
                match result with
                | Error error -> Some error
                | Ok() -> None

            onSaveProduct (product, optionalError)
        )

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
                                prop.key "image-input-div"
                                prop.children [
                                    Daisy.fieldsetLabel [ prop.key "image-label"; prop.text translations.Product.ImageUrl ]
                                    Daisy.input [
                                        prop.key "image-input-column"
                                        prop.className "mb-4 w-full"
                                        prop.placeholder translations.Product.ImageUrl
                                        prop.value product.ImageUrl
                                        yield! propOnChangeOrReadonly (fun url -> dispatch (ProductChanged { product with ImageUrl = url }))
                                    ]

                                    Daisy.fieldsetLabel [ prop.key "name-label"; prop.text translations.Product.Name ]
                                    Daisy.input [
                                        prop.key "name-input"
                                        prop.className "mb-4 w-full"
                                        prop.placeholder translations.Product.Name
                                        prop.value product.Name
                                        yield! propOnChangeOrReadonly (fun name -> dispatch (ProductChanged { product with Name = name }))
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
                        prop.className "h-21 mb-4 w-full"
                        prop.placeholder translations.Product.Description
                        prop.value product.Description
                        yield! propOnChangeOrReadonly (fun description -> dispatch (ProductChanged { product with Description = description }))
                    ]

                    match catalogAccess with
                    | None
                    | Some View -> ()
                    | Some Edit ->
                        Daisy.button.button [
                            button.primary
                            prop.className "justify-self-start"
                            prop.key "save-product-button"

                            prop.children [
                                Html.text translations.Product.Save

                                match model.SaveDate with
                                | Remote.Empty -> ()
                                | Remote.Loading -> Daisy.loading [ loading.spinner; prop.key "save-product-spinner" ]
                                | Remote.LoadError apiError ->
                                    Daisy.tooltip [
                                        tooltip.text (translations.Product.SaveError(product.SKU, apiError.ErrorMessage))
                                        tooltip.right
                                        tooltip.error
                                        prop.text "❗"
                                        prop.key "save-product-error-tooltip"
                                    ]
                                | Remote.Loaded dateTime ->
                                    Daisy.tooltip [
                                        tooltip.text $"%s{translations.Product.SaveOk(product.SKU)} @ {dateTime}"
                                        tooltip.right
                                        tooltip.success
                                        prop.key "save-product-ok-tooltip"
                                        prop.children [
                                            Html.span [
                                                prop.key "save-product-ok-text"
                                                prop.text "✓"
                                                prop.className "font-bold text-green-500"
                                            ]
                                        ]
                                    ]
                            ]

                            match model.SaveDate with
                            | Remote.Loading -> prop.disabled true
                            | _ -> prop.onClick (fun _ -> dispatch (SaveProduct(product, Start)))
                        ]
                ]
            ]
    ]