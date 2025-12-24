module Shopfoo.Client.Pages.Product.ModifyPrice

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

let private CountdownStep = TimeSpan.FromMilliseconds 100.
let private CountdownDuration = 50. * CountdownStep

type private Msg = // ↩
    | PriceChanged of PriceModel
    | SavePrices of ApiCall<unit>
    | StartCountdown
    | StopCountdown
    | DecrementCountdown

type private Model = {
    Price: PriceModel
    Prices: Prices
    SaveDate: Remote<DateTime>
    Countdown: TimeSpan option
} with
    /// Closes the drawer, passing the modified prices back to drawer opener, only if prices have been saved.
    member model.CloseDrawer(drawerControl: DrawerControl) =
        let drawer =
            match model.SaveDate with
            | Remote.Loaded _ -> Some(Drawer.ModifyPrice(model.Price, model.Prices))
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
        Countdown = None
    },
    Cmd.none

let private update (fullContext: FullContext) (drawerControl: DrawerControl) onSave (msg: Msg) (model: Model) =
    match msg with
    | PriceChanged price -> // ↩
        { model with Price = price; Prices = price.Update(model.Prices) }, Cmd.none

    | SavePrices Start ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.savePrices (fullContext.PrepareRequest model.Prices)

    | SavePrices(Done result) ->
        let saveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult

        { model with SaveDate = saveDate },
        Cmd.batch [
            Cmd.ofEffect (fun _ -> onSave (model.Prices, result |> Result.tryGetError))
            if result.IsOk then
                Cmd.ofMsg StartCountdown
        ]

    | StartCountdown ->
        { model with Countdown = Some CountdownDuration }, // ↩
        Cmd.ofMsgDelayed DecrementCountdown CountdownStep

    | StopCountdown -> // ↩
        { model with Countdown = None }, Cmd.none

    | DecrementCountdown ->
        match model.Countdown |> Option.map (fun x -> x - CountdownStep) with
        | None -> model, Cmd.none
        | Some(TimeSpan.TotalSeconds(IsPositive) as countdown) ->
            { model with Countdown = Some countdown }, // ↩
            Cmd.ofMsgDelayed DecrementCountdown CountdownStep
        | Some _ ->
            do model.CloseDrawer(drawerControl)
            { model with Countdown = None }, Cmd.none

[<ReactComponent>]
let ModifyPriceForm key (fullContext: FullContext) price prices drawerControl onSave =
    let model, dispatch =
        React.useElmish (init price prices, update fullContext drawerControl onSave, [||])

    let currency = model.Price.Value.Currency.Symbol
    let translations = fullContext.Translations

    let currentPrice = price.Value.Value
    let newPrice = model.Price.Value.Value
    let priceChange = newPrice - currentPrice
    let percentChange = round (100m * priceChange / currentPrice)

    let currencyLabel priceKey symbol =
        Daisy.label [ prop.key $"%s{key}-%s{priceKey}-symbol-left"; prop.text $"%s{symbol}" ]

    Daisy.fieldset [
        prop.key $"%s{key}-fieldset"
        prop.children [
            Html.legend [
                prop.key $"%s{key}-legend"
                prop.className "text-base font-bold mb-2"
                prop.children [
                    match price.Intent with
                    // TODO: Use icons from Shopfoo.Client.Components.Icons
                    | Increase -> Html.text $"↗️ %s{translations.Product.Increase}"
                    | Decrease -> Html.text $"↘️ %s{translations.Product.Decrease}"

                    Html.text " • "

                    match price.Type with
                    | ListPrice -> Html.text translations.Product.ListPrice
                    | RetailPrice -> Html.text translations.Product.RetailPrice
                ]
            ]

            // -- CurrentPrice ----
            Daisy.fieldsetLabel [ prop.key $"%s{key}-current-price-fieldset-label"; prop.text translations.Product.CurrentPrice ]
            Daisy.label.input [
                prop.key $"{key}-current-price-label"
                prop.className "bg-base-300 w-full mb-2"
                prop.children [
                    match currency with
                    | Symbol.Left symbol -> currencyLabel "current-price" symbol
                    | _ -> ()

                    Html.input [
                        prop.key $"%s{key}-current-price-input"
                        prop.readOnly true
                        prop.value (currentPrice |> float)
                    ]

                    match currency with
                    | Symbol.Right symbol -> currencyLabel "current-price" symbol
                    | _ -> ()
                ]
            ]

            // -- NewPrice ----
            Daisy.fieldsetLabel [
                prop.key $"%s{key}-new-price-fieldset-label"
                prop.children [
                    Html.text translations.Product.NewPrice

                    if priceChange <> 0m then
                        let changeText, signSymbol =
                            match price.Intent with
                            | Increase -> translations.Product.Increase, "+"
                            | Decrease -> translations.Product.Decrease, ""

                        Html.div [
                            prop.key $"%s{key}-price-percent-change"
                            prop.className "ml-auto"
                            prop.text $"%s{changeText}%s{translations.Home.Colon} %s{signSymbol}%.0f{percentChange}%%"
                        ]
                ]
            ]
            Daisy.label.input [
                prop.key $"{key}-new-price-label"
                prop.className "w-full mb-4"
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
                        prop.onChange (fun (value: float) ->
                            dispatch (PriceChanged { model.Price with Value = model.Price.Value.WithValue(decimal value) })
                        )

                        match price.Intent with
                        | Increase -> // ↩
                            prop.min (float currentPrice)
                        | Decrease ->
                            prop.max (float currentPrice)
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
                        prop.onClick (fun _ ->
                            model.CloseDrawer(drawerControl)
                            dispatch StopCountdown
                        )
                        prop.children [
                            Html.text translations.Home.Close

                            match model.Countdown with
                            | None -> ()
                            | Some(TimeSpan.TotalSeconds seconds) ->
                                let percent = int (100. * seconds / CountdownDuration.TotalSeconds)

                                Daisy.radialProgress [
                                    color.textSuccess
                                    prop.style [ style.custom ("--value", percent); style.custom ("--size", "2rem") ]
                                    prop.text $"%.0f{ceil seconds}s"
                                    prop.key $"%s{key}-save-countdown"
                                ]
                        ]
                    ]

                    // -- Cancel Countdown ----
                    match model.Countdown with
                    | None -> ()
                    | Some _ ->
                        Daisy.button.button [
                            button.accent
                            button.outline
                            prop.key $"%s{key}-cancel-button"
                            prop.text translations.Home.Cancel
                            prop.className "mr-2"
                            prop.onClick (fun _ -> dispatch StopCountdown)
                        ]

                    // -- Save Prices ----
                    // TODO: [After Save] hide Save button and disable form fields
                    Buttons.SaveButton(
                        key = "save-price",
                        label = translations.Home.Save,
                        tooltipOk = translations.Home.SaveOk priceLabel,
                        tooltipError = (fun err -> translations.Home.SaveError(priceLabel, err.ErrorMessage)),
                        tooltipProps = [ tooltip.left ],
                        saveDate = model.SaveDate,
                        disabled = ((abs priceChange) < 0.01m),
                        onClick = (fun () -> dispatch (SavePrices Start))
                    )
                ]
            ]
        ]
    ]