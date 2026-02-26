module Shopfoo.Client.Pages.Product.Details.InputSales

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Pages.Product
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Shared.Remoting

type private Msg =
    | DateChanged of DateOnly
    | QuantityChanged of int
    | SalePriceChanged of decimal
    | InputSale of ApiCall<unit>

type private Model = {
    SKU: SKU
    Date: DateOnly
    Quantity: int
    SalePrice: decimal
    Currency: Currency
    SaveDate: Remote<DateTime>
} with
    member model.CloseDrawer(drawerControl: DrawerControl) =
        let drawer =
            match model.SaveDate with
            | Remote.Loaded _ -> Some(Drawer.InputSales model.Currency)
            | _ -> None

        drawerControl.Close(?drawer = drawer)

[<RequireQualifiedAccess>]
module private Cmd =
    let inputSale (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.InputSale request
            Error = InputSale << Done << Error
            Success = InputSale << Done << Ok
        }

let private init sku currency =
    {
        SKU = sku
        Date = DateOnly.FromDateTime(DateTime.Today)
        Quantity = 1
        SalePrice = 0m
        Currency = currency
        SaveDate = Remote.Empty
    },
    Cmd.none

let private update (fullContext: FullContext) onSave (msg: Msg) (model: Model) =
    match msg with
    | DateChanged date -> // ↩
        { model with Date = date }, Cmd.none

    | QuantityChanged quantity -> // ↩
        { model with Quantity = quantity }, Cmd.none

    | SalePriceChanged price -> // ↩
        { model with SalePrice = price }, Cmd.none

    | InputSale Start ->
        let sale: Sale = {
            SKU = model.SKU
            Date = model.Date
            Quantity = model.Quantity
            Price = Money.ByCurrency model.Currency model.SalePrice
        }

        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.inputSale (fullContext.PrepareRequest sale)

    | InputSale(Done result) ->
        { model with SaveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult },
        Cmd.ofEffect (fun _ -> onSave (model.SKU, result |> Result.tryGetError))

[<ReactComponent>]
let InputSalesForm key (sku: SKU) (currency: Currency) (fullContext: FullContext) (drawerControl: DrawerControl) onSave =
    let model, dispatch = React.useElmish (init sku currency, update fullContext onSave, [||])

    React.useEffect (fun () ->
        match model.SaveDate with
        | Remote.Loaded _ ->
            JS.runAfter // ↩
                (TimeSpan.FromMilliseconds(500))
                (fun () -> model.CloseDrawer(drawerControl))
        | _ -> ()
    )

    let translations = fullContext.Translations

    let afterSaveOk =
        match model.SaveDate with
        | Remote.Loaded _ -> true
        | _ -> false

    let canSave = model.Quantity > 0 && model.SalePrice > 0m

    let currencySymbol = model.Currency.Symbol

    Daisy.fieldset [
        prop.key $"%s{key}-fieldset"
        prop.children [
            Html.legend [
                prop.key $"%s{key}-legend"
                prop.className "text-base font-bold mb-2 flex items-center"
                prop.text translations.Product.SaleAction.InputSales
            ]

            // -- Date ----
            Daisy.fieldsetLabel [ // ↩
                prop.key $"%s{key}-date-fieldset-label"
                prop.text translations.Home.Date
            ]

            Daisy.label.input [
                prop.key $"{key}-date-label"
                prop.className [
                    "w-full mb-2"
                    if afterSaveOk then
                        "bg-base-300"
                ]
                prop.children [
                    Html.input [
                        prop.key $"%s{key}-date-input"
                        prop.type' "date"
                        prop.className "flex-1 validator"
                        prop.required true
                        prop.value (model.Date.ToDateTime().ToString("yyyy-MM-dd"))

                        if afterSaveOk then
                            prop.ariaReadOnly true
                            prop.readOnly true
                        else
                            prop.onChange (fun (value: string) ->
                                match DateOnly.TryParse(value) with
                                | true, date -> dispatch (DateChanged date)
                                | _ -> ()
                            )
                    ]
                ]
            ]

            // -- Quantity ----
            Daisy.fieldsetLabel [ prop.key $"%s{key}-quantity-fieldset-label"; prop.text translations.Product.Quantity ]

            Daisy.label.input [
                prop.key $"{key}-quantity-label"
                prop.className [
                    "w-full mb-2"
                    if afterSaveOk then
                        "bg-base-300"
                ]
                prop.children [
                    Html.input [
                        prop.key $"%s{key}-quantity-input"
                        prop.type' "number"
                        prop.className "flex-1 validator"
                        prop.required true
                        prop.value model.Quantity

                        if afterSaveOk then
                            prop.ariaReadOnly true
                            prop.readOnly true
                        else
                            prop.onChange (fun (value: int) -> dispatch (QuantityChanged value))

                            if canSave then
                                prop.onKeyUp (Feliz.key.enter, fun _ -> dispatch (InputSale Start))

                        prop.min 1
                        prop.step 1
                    ]
                ]
            ]

            // -- Sale Price ----
            Daisy.fieldsetLabel [ prop.key $"%s{key}-price-fieldset-label"; prop.text translations.Product.SalePrice ]

            Daisy.label.input [
                prop.key $"{key}-price-label"
                prop.className [
                    "w-full mb-4"
                    if afterSaveOk then
                        "bg-base-300"
                ]
                prop.children [
                    match currencySymbol with
                    | Symbol.Left symbol -> Daisy.label [ prop.key $"%s{key}-price-symbol-left"; prop.text symbol ]
                    | _ -> ()

                    Html.input [
                        prop.key $"%s{key}-price-input"
                        prop.type' "number"
                        prop.className "flex-1 validator"
                        prop.required true
                        prop.value (model.SalePrice |> float)

                        if afterSaveOk then
                            prop.ariaReadOnly true
                            prop.readOnly true
                        else
                            prop.onChange (fun (value: float) -> dispatch (SalePriceChanged(decimal value)))

                            if canSave then
                                prop.onKeyUp (Feliz.key.enter, fun _ -> dispatch (InputSale Start))

                        prop.min 0.01
                    ]

                    match currencySymbol with
                    | Symbol.Right symbol -> Daisy.label [ prop.key $"%s{key}-price-symbol-right"; prop.text symbol ]
                    | _ -> ()
                ]
            ]

            // -- Buttons ----
            Html.div [
                prop.key $"%s{key}-save-div"
                prop.className "flex justify-end"
                prop.children [
                    // -- Close ----
                    Daisy.button.button [
                        button.secondary
                        button.outline
                        prop.key $"%s{key}-close-button"
                        prop.className "mr-2"
                        prop.onClick (fun _ -> model.CloseDrawer(drawerControl))
                        prop.text translations.Home.Close
                    ]

                    // -- Save ----
                    Buttons.SaveButton(
                        key = "save-sale",
                        label = translations.Home.Save,
                        tooltipOk = translations.Home.SavedOk translations.Product.SaleAction.InputSales,
                        tooltipError = (fun err -> translations.Home.SavedError(translations.Product.SaleAction.InputSales, err.ErrorMessage)),
                        tooltipProps = [ tooltip.left ],
                        saveDate = model.SaveDate,
                        disabled = (afterSaveOk || not canSave),
                        onClick = (fun () -> dispatch (InputSale Start))
                    )
                ]
            ]
        ]
    ]