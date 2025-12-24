module Shopfoo.Client.Components.Actions

open Feliz
open Feliz.DaisyUI
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Security

type Action = {
    Key: string
    Text: string
    Icon: ReactElement option
    OnClick: unit -> unit
} with
    static member withIcon key icon text onClick : Action = {
        Key = key
        Text = text
        Icon = Some icon
        OnClick = onClick
    }

type Value =
    | Natural of value: int
    | Money of value: decimal option * currency: Symbol option

    static member OfMoney =
        function
        | Dollars value -> Money(Some value, Some(Symbol.Left "$"))
        | Euros value -> Money(Some value, Some(Symbol.Right "€"))

    static member OfMoneyOptional =
        function
        | Some money -> Value.OfMoney money
        | None -> Money(None, None)

    member this.Symbol =
        match this with
        | Value.Natural _ -> None
        | Value.Money(_, symbol) -> symbol

    member this.Text =
        match this with
        | Value.Natural n -> $"%i{n}"
        | Value.Money(None, _) -> ""
        | Value.Money(Some f, _) -> $"%0.2f{f}"

[<ReactComponent>]
let ActionsDropdown key access (value: Value) (actions: Action list) =
    let itemElement (action: Action) =
        Html.li [
            prop.key $"{key}-action--{action.Key}"
            prop.children [
                Html.a [
                    prop.key $"{key}-action--{action.Key}-link"
                    prop.className "flex items-center justify-start"
                    prop.onClick (fun _ -> action.OnClick())
                    prop.children [
                        match action.Icon with
                        | Some iconElement -> iconElement
                        | None -> ()

                        Html.span [
                            prop.key $"{key}-action--{action.Key}-text"
                            prop.className [
                                "whitespace-nowrap"
                                if action.Icon.IsSome then
                                    "ml-1"
                            ]
                            prop.text action.Text
                        ]
                    ]
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
                    | Some(Symbol.Left symbol) -> Daisy.label [ prop.key $"{key}-label-symbol"; prop.text symbol ]
                    | _ -> ()

                    Html.input [
                        prop.key $"{key}-input"
                        prop.className "flex-1"
                        prop.value value.Text
                        prop.onChange ignore<bool> // `onChange` is needed by React because it's a controlled input.
                        prop.readOnly true
                        prop.type' "text"
                    ]

                    match value.Symbol with
                    | Some(Symbol.Right symbol) -> Daisy.label [ prop.key $"{key}-label-symbol"; prop.text symbol ]
                    | _ -> ()
                ]
            ]

            match actions, access with
            | [], _
            | _, None
            | _, Some Access.View -> ()
            | _, Some Access.Edit ->
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
        ]
    ]