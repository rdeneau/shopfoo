module Shopfoo.Client.Pages.Product.Details

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client.Pages.Product.CatalogInfo

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
    Html.section [
        prop.key "product-details-page"
        prop.className "grid grid-cols-4 gap-4"
        prop.children [
            Html.div [
                prop.key "index-page-product-details"
                prop.className "col-span-3"
                prop.children [ CatalogInfoSection("catalog-info", fullContext, sku, fillTranslations) ]
            ]
            Html.div [
                prop.key "index-page-product-actions"
                prop.className "col-span-1"
                prop.children Section.ProductActions
            ]
        ]
    ]