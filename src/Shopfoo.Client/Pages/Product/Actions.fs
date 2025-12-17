module Shopfoo.Client.Pages.Product.Actions

open Feliz
open Feliz.DaisyUI

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

type private Value =
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

[<ReactComponent>]
let private ActionsDropdown key (value: Value) (actions: Action list) =
    Html.div [
        prop.key $"%s{key}-div"
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

[<ReactComponent>]
let ActionsForm =
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
            ActionsDropdown "price" (Value.Euros 85.00m) [
                Action.Emoji("↗️", "increase", "Increase Price", fun () -> ()) // TODO
                Action.Emoji("↘️", "decrease", "Decrease Price", fun () -> ()) // TODO
                Action.Emoji("🚫", "unavailable", "Unavailable", fun () -> ()) // TODO
                Action.Emoji("📦", "free-shipping", "Free Shipping", fun () -> ()) // TODO
            ]

            Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text "Stock" ]
            ActionsDropdown "stock" (Value.Natural 17) [
                Action.Emoji("✏️", "inventory-adjustment", "Inventory Adjustment", fun () -> ()) // TODO
            ]
        ]
    ]