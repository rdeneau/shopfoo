module Shopfoo.Client.Pages.Product.Details.ReceiveSupply

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
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Shared.Remoting

type private Msg =
    | DateChanged of DateOnly
    | QuantityChanged of int
    | PurchasePriceChanged of decimal
    | ReceiveSupply of ApiCall<unit>

type private Model = {
    SKU: SKU
    Date: DateOnly
    Quantity: int
    PurchasePrice: decimal
    Currency: Currency
    SaveDate: Remote<DateTime>
} with
    /// Closes the drawer, passing the modified data back to drawer opener, only if it has been saved.
    member model.CloseDrawer(drawerControl: DrawerControl) =
        let drawer =
            match model.SaveDate with
            | Remote.Loaded _ -> Some(Drawer.ReceivePurchasedProducts model.Currency)
            | _ -> None

        drawerControl.Close(?drawer = drawer)

[<RequireQualifiedAccess>]
module private Cmd =
    let receiveSupply (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.ReceiveSupply request
            Error = ReceiveSupply << Done << Error
            Success = ReceiveSupply << Done << Ok
        }

let private init sku currency =
    {
        SKU = sku
        Date = DateOnly.FromDateTime(DateTime.Today)
        Quantity = 1
        PurchasePrice = 0m
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

    | PurchasePriceChanged price -> // ↩
        { model with PurchasePrice = price }, Cmd.none

    | ReceiveSupply Start ->
        let input: ReceiveSupplyInput = {
            SKU = model.SKU
            Date = model.Date
            Quantity = model.Quantity
            PurchasePrice = Money.ByCurrency model.Currency model.PurchasePrice
        }

        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.receiveSupply (fullContext.PrepareRequest input)

    | ReceiveSupply(Done result) ->
        { model with SaveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult },
        Cmd.ofEffect (fun _ -> onSave (model.SKU, result |> Result.tryGetError))

[<ReactComponent>]
let ReceiveSupplyForm key (sku: SKU) (currency: Currency) (fullContext: FullContext) (drawerControl: DrawerControl) onSave =
    let model, dispatch = React.useElmish (init sku currency, update fullContext onSave, [||])

    // Close the drawer after success with a short delay (500ms),
    // time to let the user get this result visually.
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

    let canSave = model.Quantity > 0 && model.PurchasePrice > 0m

    let currencySymbol = model.Currency.Symbol

    Daisy.fieldset [
        prop.key $"%s{key}-fieldset"
        prop.children [
            Html.legend [
                prop.key $"%s{key}-legend"
                prop.className "text-base font-bold mb-2 flex items-center"
                prop.text translations.Product.StockAction.ReceivePurchasedProducts
            ]

            // -- Date ----
            Daisy.fieldsetLabel [ // ↩
                prop.key $"%s{key}-date-fieldset-label"
                prop.text translations.Product.SupplyDate
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
            Daisy.fieldsetLabel [ prop.key $"%s{key}-quantity-fieldset-label"; prop.text translations.Product.SupplyQuantity ]

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
                                prop.onKeyUp (Feliz.key.enter, fun _ -> dispatch (ReceiveSupply Start))

                        prop.min 1
                        prop.step 1
                    ]
                ]
            ]

            // -- Purchase Price ----
            Daisy.fieldsetLabel [ prop.key $"%s{key}-price-fieldset-label"; prop.text translations.Product.SupplyPurchasePrice ]

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
                        prop.value (model.PurchasePrice |> float)

                        if afterSaveOk then
                            prop.ariaReadOnly true
                            prop.readOnly true
                        else
                            prop.onChange (fun (value: float) -> dispatch (PurchasePriceChanged(decimal value)))

                            if canSave then
                                prop.onKeyUp (Feliz.key.enter, fun _ -> dispatch (ReceiveSupply Start))

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
                        key = "save-supply",
                        label = translations.Home.Save,
                        tooltipOk = translations.Home.SavedOk translations.Product.Stock,
                        tooltipError = (fun err -> translations.Home.SavedError(translations.Product.Stock, err.ErrorMessage)),
                        tooltipProps = [ tooltip.left ],
                        saveDate = model.SaveDate,
                        disabled = (afterSaveOk || not canSave),
                        onClick = (fun () -> dispatch (ReceiveSupply Start))
                    )
                ]
            ]
        ]
    ]
