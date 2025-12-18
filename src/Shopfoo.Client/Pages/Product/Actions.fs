module Shopfoo.Client.Pages.Product.Actions

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Remoting

type private Model = { Prices: Remote<Prices>; SaveStatus: Remote<unit> }

type private Msg = PricesFetched of ApiResult<GetPricesResponse>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadPrices (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.GetPrices request
            Error = Error >> PricesFetched
            Success = Ok >> PricesFetched
        }

let private init (fullContext: FullContext) sku =
    { Prices = Remote.Loading; SaveStatus = Remote.Empty }, // ↩
    Cmd.loadPrices (fullContext.PrepareRequest sku)

let private update (fullContext: FullContext) (msg: Msg) (model: Model) =
    match msg with
    | PricesFetched(Ok response) -> { model with Prices = response.Prices |> Remote.ofOption }, Cmd.none
    | PricesFetched(Error apiError) -> { model with Prices = Remote.LoadError apiError }, Cmd.none

// -- View ----

type private Action = {
    Key: string
    Text: string
    OnClick: unit -> unit
} with
    static member Emoji(emoji, key, text, onClick: unit -> unit) : Action = {
        Key = key
        Text = $"%s{emoji}  {text}"
        OnClick = onClick
    }

[<RequireQualifiedAccess>]
type private Symbol =
    | None
    | Left of string
    | Right of string

type private Value =
    | Natural of value: int
    | Money of value: decimal * currency: Symbol

    static member OfMoney =
        function
        | Dollars value -> Money(value, Symbol.Left "$")
        | Euros value -> Money(value, Symbol.Right "€")

    member this.Symbol =
        match this with
        | Value.Natural _ -> Symbol.None
        | Value.Money(_, symbol) -> symbol

    member this.Text =
        match this with
        | Value.Natural value -> $"%i{value}"
        | Value.Money(value, _) -> $"%0.2f{value}"

[<ReactComponent>]
let private ActionsDropdown key access (value: Value) (actions: Action list) =
    let itemElement (action: Action) =
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

    Html.div [
        prop.key $"%s{key}-div"
        prop.className "flex items-center mb-4 w-full"
        prop.children [
            Daisy.label.input [
                prop.key $"{key}-label-input"
                prop.className "bg-base-300 flex-1"
                prop.children [
                    match value.Symbol with
                    | Symbol.Left symbol -> Daisy.label [ prop.key $"{key}-label-symbol"; prop.text symbol ]
                    | _ -> ()

                    Html.input [
                        prop.key $"{key}-input"
                        prop.className "flex-1"
                        prop.defaultValue value.Text
                        prop.readOnly true
                        prop.type' "text"
                    ]

                    match value.Symbol with
                    | Symbol.Right symbol -> Daisy.label [ prop.key $"{key}-label-symbol"; prop.text symbol ]
                    | _ -> ()
                ]
            ]

            match access with
            | Some Access.Edit ->
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
                                            itemElement action
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            | _ -> ()
        ]
    ]

[<ReactComponent>]
let ActionsForm key fullContext sku =
    let model, dispatch =
        React.useElmish (init fullContext sku, update fullContext, [||])

    let translations = fullContext.Translations

    React.fragment [
        match model.Prices with
        | Remote.Empty -> ()
        | Remote.Loading -> Daisy.skeleton [ prop.className "h-64 w-full"; prop.key "prices-skeleton" ]
        | Remote.LoadError apiError -> Alert.apiError "prices-load-error" apiError fullContext.User
        | Remote.Loaded prices ->
            Daisy.fieldset [
                prop.key $"%s{key}-fieldset"
                prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                prop.children [
                    Html.legend [
                        prop.key "product-actions-legend"
                        prop.className "text-sm"
                        prop.text $"⚡ %s{translations.Product.Actions}"
                    ]

                    Daisy.fieldsetLabel [ prop.key "retail-price-label"; prop.text translations.Product.RetailPrice ]
                    ActionsDropdown "retail-price" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoney prices.RetailPrice) [
                        Action.Emoji("↗️", "increase", translations.Product.PriceAction.Increase, fun () -> ()) // TODO
                        Action.Emoji("↘️", "decrease", translations.Product.PriceAction.Decrease, fun () -> ()) // TODO
                        Action.Emoji("🚫", "unavailable", translations.Product.PriceAction.Unavailable, fun () -> ()) // TODO
                    ]

                    Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text translations.Product.Stock ]
                    ActionsDropdown "stock" (fullContext.User.AccessTo Feat.Warehouse) (Value.Natural 17) [
                        Action.Emoji("✏️", "inventory-adjustment", translations.Product.StockAction.InventoryAdjustment, fun () -> ()) // TODO
                    ]
                ]
            ]
    ]