module Shopfoo.Client.Components.Lang

open Feliz
open Feliz.DaisyUI
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types

type LangMenu = {
    Lang: Lang
    Code: string
    Label: string
    Emoji: string
    Status: Remote<unit>
}

[<RequireQualifiedAccess>]
module LangMenu =
    let private mk lang code label emoji : LangMenu = {
        Lang = lang
        Code = code
        Label = label
        Emoji = emoji
        Status = Remote.Loaded()
    }

    let all = [
        mk Lang.English "en" "English" "ᴇɴ"
        mk Lang.French "fr" "Français" "ꜰʀ"
        mk Lang.Latin "la" "Latin ⚠️" "ʟᴀ"
    ]

[<AutoOpen>]
module private LangExtensions =
    type Lang with
        member lang.AsModel = // ↩
            LangMenu.all |> List.find (fun lg -> lg.Lang = lang)

type private LangMenuElement(currentLang, onClick) =
    member private _.li(key, langMenu: LangMenu, canClick, ?isSelected) =
        fun statusElement ->
            let isSelected = defaultArg isSelected false

            Html.li [
                prop.key $"%s{key}-li"
                prop.className "aria-selected:bg-base-300 rounded"
                prop.ariaSelected isSelected
                prop.children [
                    Html.a [
                        prop.key $"{key}-link"
                        prop.className [
                            "whitespace-nowrap"
                            if not canClick then
                                "cursor-default"
                                "opacity-60"
                        ]

                        if canClick then
                            prop.onClick (fun _ ->
                                onClick langMenu.Lang
                                JS.blurActiveElement ()
                            )
                        else
                            prop.ariaDisabled true

                        prop.children [
                            Html.span [ // ↩
                                prop.key $"{key}-label"
                                prop.text $"%s{langMenu.Emoji}  %s{langMenu.Label}"
                            ]
                            statusElement
                        ]
                    ]
                ]
            ]

    member this.item(langMenu: LangMenu) =
        let key = $"lang-%s{langMenu.Code}"

        let isCurrentLang = // ↩
            langMenu.Lang = currentLang

        match langMenu.Status with
        | Remote.Empty -> Html.none
        | Remote.Loading ->
            Daisy.loading [
                prop.key $"{key}-spinner"
                loading.spinner
                loading.xs
                color.textInfo
            ]
            |> this.li (key, langMenu, canClick = false)

        | Remote.Loaded() ->
            Html.span [
                prop.key $"{key}-status"
                prop.className "ml-auto font-bold text-green-500 min-w-[1em]"
                if isCurrentLang then
                    prop.children (icon fa6Solid.check)
            ]
            |> this.li (key, langMenu, canClick = not isCurrentLang, isSelected = isCurrentLang)

        | Remote.LoadError apiError ->
            Daisy.tooltip [ // ↩
                prop.key $"{key}-error-tooltip"
                color.textError
                prop.children (icon fa6Solid.circleExclamation)
                tooltip.text $"Error: %s{apiError.ErrorMessage}"
            ]
            |> this.li (key, langMenu, canClick = true)

[<ReactComponent>]
let LangDropdown key (currentLang: Lang) menus onClick =
    Daisy.dropdown [
        dropdown.hover
        dropdown.end'
        prop.key $"%s{key}-dropdown"
        prop.className "flex-none"
        prop.children [
            Daisy.button.button [
                button.ghost
                prop.key "lang-button"
                prop.className "opacity-80 hover:opacity-100"
                prop.children (icon fa6Solid.language)
            ]
            Daisy.dropdownContent [
                prop.key "lang-dropdown-content"
                prop.className "p-2 shadow menu bg-base-100 rounded-box"
                prop.tabIndex 0
                prop.children [
                    Html.ul [
                        prop.key "lang-dropdown-list"
                        prop.children [ // ↩
                            let menuElement = LangMenuElement(currentLang, onClick)

                            for menu in menus do
                                menuElement.item menu
                        ]
                    ]
                ]
            ]
        ]
    ]