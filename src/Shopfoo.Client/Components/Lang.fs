module Shopfoo.Client.Components.Lang

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
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
        mk Lang.English "en" "English" "🇬🇧"
        mk Lang.French "fr" "Français" "🇫🇷"
        mk Lang.Latin "la" "Latin" "🚩"
    ]

[<AutoOpen>]
module private LangExtensions =
    type Lang with
        member lang.AsModel = // ↩
            LangMenu.all |> List.find (fun lg -> lg.Lang = lang)

type private LangMenuElement(currentLang, onClick) =
    member private _.li(key, lg: LangMenu, canClick) =
        fun statusElement ->
            Html.li [
                prop.key $"%s{key}-li"
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
                                onClick lg.Lang

                                // Remove the focus to fix the menu hiding on mouse out
                                match Browser.Dom.document.activeElement with
                                | :? Browser.Types.HTMLElement as el -> el.blur ()
                                | _ -> ()
                            )
                        else
                            prop.ariaDisabled true

                        prop.children [ // ↩
                            Html.span [ prop.key $"{key}-label"; prop.text $"%s{lg.Emoji} %s{lg.Label}" ]
                            statusElement
                        ]
                    ]
                ]
            ]

    member this.item(lg: LangMenu) =
        let key = $"lang-%s{lg.Code}"

        let isCurrentLang = // ↩
            lg.Lang = currentLang

        match lg.Status with
        | Remote.Empty -> Html.none
        | Remote.Loading ->
            Daisy.loading [
                prop.key $"{key}-spinner"
                loading.spinner
                loading.xs
                color.textInfo
            ]
            |> this.li (key, lg, canClick = false)

        | Remote.Loaded() ->
            Html.span [
                prop.key $"{key}-status"
                prop.className "ml-auto font-bold text-green-500 min-w-[1em]"
                prop.text (if isCurrentLang then "✓" else "")
            ]
            |> this.li (key, lg, canClick = not isCurrentLang)

        | Remote.LoadError apiError ->
            Daisy.tooltip [ // ↩
                tooltip.text $"Error: %s{apiError.ErrorMessage}"
                prop.key $"{key}-error-tooltip"
                prop.child (Daisy.status [ status.error; status.xl ])
            ]
            |> this.li (key, lg, canClick = true)

[<ReactComponent>]
let LangDropdown (key, currentLang: Lang, menus, onClick) =
    Daisy.dropdown [
        dropdown.hover
        dropdown.end'
        prop.key $"%s{key}-dropdown"
        prop.className "flex-none"
        prop.children [
            Daisy.button.button [
                button.ghost
                prop.key "lang-button"
                prop.text currentLang.AsModel.Emoji
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