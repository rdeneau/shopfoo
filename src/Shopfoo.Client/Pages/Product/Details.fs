module Shopfoo.Client.Pages.Product.Details

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Shared.Remoting

type private Model = unit

type private Msg = unit

let private init () = (), Cmd.none

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | () -> model, Cmd.none

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
    let ProductCatalogInfo =
        Daisy.fieldset [
            prop.key "product-details-fieldset"
            prop.className "bg-base-200 border border-base-300 rounded-box p-4"
            prop.children [
                Html.legend [ prop.key "product-details-legend"; prop.text "🗂️ Catalog Info" ]

                Daisy.fieldsetLabel [ prop.key "sku-label"; prop.text "SKU" ]
                Html.p [
                    prop.key "sku-value"
                    prop.className "font-bold mb-4"
                    prop.text "0321125215"
                ]

                Daisy.fieldsetLabel [ prop.key "name-label"; prop.text "Name" ]
                Daisy.input [
                    prop.key "name-input"
                    prop.placeholder "Name"
                    prop.className "mb-4 w-full"
                    prop.onChange (fun (value: string) -> ()) // TODO
                    prop.value "Domain-Driven Design: Tackling Complexity in the Heart of Software"
                ]

                Daisy.fieldsetLabel [ prop.key "description-label"; prop.text "Description" ]
                Daisy.textarea [
                    prop.key "description-textarea"
                    prop.placeholder "Description"
                    prop.className "h-21 mb-4 w-full"
                    prop.onChange (fun (value: string) -> ()) // TODO
                    prop.value (
                        "Leading software designers have recognized domain modeling and design as critical topics for at least twenty years, "
                        + "yet surprisingly little has been written about what needs to be done or how to do it. Although it has never been "
                        + "clearly formulated, a philosophy has developed as an undercurrent in the object community, which I call 'domain-driven design'."
                    )
                ]

                Daisy.button.button [
                    button.primary
                    prop.className "justify-self-start"
                    prop.key "save-product-button"
                    prop.type' "submit"
                    prop.text "Save"
                    prop.onClick (fun _ -> ()) // TODO
                ]
            ]
        ]

    let ProductActions =
        Daisy.fieldset [
            prop.key "product-actions-fieldset"
            prop.className "bg-base-200 border border-base-300 rounded-box p-4"
            prop.children [
                Html.legend [ prop.key "product-actions-legend"; prop.text "⚡ Actions" ]

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
let DetailsView (fullContext: ReactState<FullContext>, sku) =
    let state, dispatch = React.useElmish (init, update, [||])

    Html.div [
        prop.key "product-details-page"
        prop.className "grid grid-cols-4 gap-4"
        prop.children [
            Html.div [
                prop.key "index-page-product-details"
                prop.className "col-span-3"
                prop.children Section.ProductCatalogInfo
            ]
            Html.div [
                prop.key "index-page-product-actions"
                prop.className "col-span-1"
                prop.children Section.ProductActions
            ]
        ]
    ]