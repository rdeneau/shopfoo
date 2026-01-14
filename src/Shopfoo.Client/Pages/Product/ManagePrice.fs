module Shopfoo.Client.Pages.Product.ManagePrice

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Shared.Remoting

type private Msg = // ↩
    | PriceChanged of PriceModel
    | SavePrices of ApiCall<unit>

type private Model = {
    Price: PriceModel
    Prices: Prices
    SaveDate: Remote<DateTime>
} with
    /// Closes the drawer, passing the modified prices back to drawer opener, only if prices have been saved.
    member model.CloseDrawer(drawerControl: DrawerControl) =
        let drawer =
            match model.SaveDate with
            | Remote.Loaded _ -> Some(Drawer.ManagePrice(model.Price, model.Prices))
            | _ -> None

        drawerControl.Close(?drawer = drawer)

[<RequireQualifiedAccess>]
module private Cmd =
    let savePrices (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.SavePrices request
            Error = SavePrices << Done << Error
            Success = SavePrices << Done << Ok
        }

let private init price prices =
    {
        Price = price
        Prices = prices
        SaveDate = Remote.Empty
    },
    Cmd.none

let private update (fullContext: FullContext) onSave (msg: Msg) (model: Model) =
    match msg with
    | PriceChanged price -> // ↩
        { model with Price = price; Prices = price.Update(model.Prices) }, Cmd.none

    | SavePrices Start ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.savePrices (fullContext.PrepareRequest model.Prices)

    | SavePrices(Done result) ->
        { model with SaveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult },
        Cmd.ofEffect (fun _ -> onSave (model.Prices, result |> Result.tryGetError))

[<ReactComponent>]
let ManagePriceForm key (fullContext: FullContext) price prices drawerControl onSave =
    let model, dispatch = React.useElmish (init price prices, update fullContext onSave, [||])

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

    let currency = model.Price.Value.Currency.Symbol
    let translations = fullContext.Translations

    let currentPrice = price.Value.Value
    let newPrice = model.Price.Value.Value
    let priceChange = newPrice - currentPrice
    let percentChange = round (100m * priceChange / currentPrice)

    let afterSaveOk =
        match model.SaveDate with
        | Remote.Loaded _ -> true
        | _ -> false

    let canSave =
        match price.Intent with
        | Increase
        | Decrease -> (abs priceChange) >= 0.01m
        | Define -> newPrice >= 0.01m

    let currencyLabel priceKey symbol = Daisy.label [ prop.key $"%s{key}-%s{priceKey}-symbol-left"; prop.text $"%s{symbol}" ]

    let icon, title =
        match price.Intent with
        | Increase -> PriceAction.Icons.increase, translations.Product.Increase
        | Decrease -> PriceAction.Icons.decrease, translations.Product.Decrease
        | Define -> PriceAction.Icons.define, translations.Product.Define

    Daisy.fieldset [
        prop.key $"%s{key}-fieldset"
        prop.children [
            Html.legend [
                prop.key $"%s{key}-legend"
                prop.className "text-base font-bold mb-2 flex items-center"
                prop.children [
                    icon
                    Html.span [
                        prop.key $"%s{key}-legend-title"
                        prop.className "ml-1"
                        prop.text title
                    ]
                    Html.span [
                        prop.key $"%s{key}-legend-separator"
                        prop.className "mx-1"
                        prop.text "•"
                    ]

                    match price.Type with
                    | ListPrice -> Html.text translations.Product.ListPrice
                    | RetailPrice -> Html.text translations.Product.RetailPrice
                ]
            ]

            // -- CurrentPrice ----
            if price.Intent <> Define then
                Daisy.fieldsetLabel [ // ↩
                    prop.key $"%s{key}-current-price-fieldset-label"
                    prop.text translations.Product.CurrentPrice
                ]

                Daisy.label.input [
                    prop.key $"{key}-current-price-label"
                    prop.className "bg-base-300 w-full mb-2"
                    prop.children [
                        match currency with
                        | Symbol.Left symbol -> currencyLabel "current-price" symbol
                        | _ -> ()

                        Html.input [
                            prop.key $"%s{key}-current-price-input"
                            prop.ariaReadOnly true
                            prop.readOnly true
                            prop.value (currentPrice |> float)
                        ]

                        match currency with
                        | Symbol.Right symbol -> currencyLabel "current-price" symbol
                        | _ -> ()
                    ]
                ]

            // -- NewPrice ----
            let displayPrecentChange changeText signSymbol =
                Html.div [
                    prop.key $"%s{key}-price-percent-change"
                    prop.className "ml-auto"
                    prop.text $"%s{changeText}%s{translations.Home.Colon} %s{signSymbol}%.0f{percentChange}%%"
                ]

            Daisy.fieldsetLabel [
                prop.key $"%s{key}-new-price-fieldset-label"
                prop.children [
                    Html.text translations.Product.NewPrice

                    match priceChange, price.Intent with
                    | 0m, _ -> ()
                    | _, Define -> ()
                    | _, Increase -> displayPrecentChange translations.Product.Increase "+"
                    | _, Decrease -> displayPrecentChange translations.Product.Decrease ""
                ]
            ]

            Daisy.label.input [
                prop.key $"{key}-new-price-label"
                prop.className [
                    "w-full mb-4"
                    if afterSaveOk then
                        "bg-base-300"
                ]
                prop.children [
                    match currency with
                    | Symbol.Left symbol -> currencyLabel "new-price" symbol
                    | _ -> ()

                    Html.input [
                        prop.key $"%s{key}-new-price-input"
                        prop.type' "number"
                        prop.className "flex-1 validator"
                        prop.required true

                        // Model/PriceToModify/Money/decimal/float
                        prop.value (newPrice |> float)
                        if afterSaveOk then
                            prop.ariaReadOnly true
                            prop.readOnly true
                        else
                            prop.onChange (fun (value: float) ->
                                dispatch (PriceChanged { model.Price with Value = model.Price.Value.WithValue(decimal value) })
                            )

                            if canSave then
                                prop.onKeyUp (Feliz.key.enter, fun _ -> dispatch (SavePrices Start))

                        match price.Intent with
                        | Increase -> // ↩
                            prop.min (float currentPrice)
                        | Decrease ->
                            prop.max (float currentPrice)
                            prop.min 0.01
                        | Define -> // ↩
                            prop.min 0.01
                    ]

                    match currency with
                    | Symbol.Right symbol -> currencyLabel "new-price" symbol
                    | _ -> ()
                ]
            ]

            // -- Buttons ----
            let priceLabel =
                match price.Type with
                | ListPrice -> translations.Product.ListPrice
                | RetailPrice -> translations.Product.RetailPrice

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

                    // -- Save Prices ----
                    Buttons.SaveButton(
                        key = "save-price",
                        label = translations.Home.Save,
                        tooltipOk = translations.Home.SaveOk priceLabel,
                        tooltipError = (fun err -> translations.Home.SaveError(priceLabel, err.ErrorMessage)),
                        tooltipProps = [ tooltip.left ],
                        saveDate = model.SaveDate,
                        disabled = (afterSaveOk || not canSave),
                        onClick = (fun () -> dispatch (SavePrices Start))
                    )
                ]
            ]
        ]
    ]