module Shopfoo.Client.Components.Theme

open System
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type private ThemeGroup =
    | Dark
    | Light

[<RequireQualifiedAccess>]
type Theme =
    | Dark
    | Light
    | Business
    | Corporate

type private Emoji = Emoji of string

let private keyOf x = $"{x}".ToLowerInvariant()

type Theme with
    member theme.ApplyOnHtml(?delay: TimeSpan) =
        let setTheme () =
            Browser.Dom.document.documentElement.setAttribute ("data-theme", keyOf theme)

        // Warning: setTheme works only when executed after the JS loop (React constraint?)
        setTheme |> JS.runAfter (defaultArg delay TimeSpan.Zero)

type private ThemeMenu(currentTheme, onClick) =
    member _.group (themeGroup: ThemeGroup) (text: string) =
        Daisy.menuTitle [ // ↩
            prop.key $"%s{keyOf themeGroup}-theme-group"
            prop.text text
        ]

    member _.item (theme: Theme) (Emoji emoji) (text: string) =
        let key = keyOf theme

        Html.li [
            prop.key $"{key}-theme"
            prop.children [
                Html.a [
                    prop.key $"{key}-theme-link"
                    prop.className "whitespace-nowrap"
                    prop.onClick (fun _ ->
                        onClick theme

                        // Remove the focus to fix the menu hiding on mouse out
                        match Browser.Dom.document.activeElement with
                        | :? Browser.Types.HTMLElement as el -> el.blur ()
                        | _ -> ()
                    )
                    prop.children [
                        Html.span [ prop.key $"{key}-theme-emoji"; prop.text emoji ]
                        Html.span [
                            prop.key $"{key}-theme-text"
                            prop.text text
                            prop.custom ("data-theme", key)
                        ]
                        Html.span [
                            prop.key $"{key}-theme-tick"
                            prop.className "ml-auto font-bold text-green-500 min-w-[1em]"
                            prop.text (if theme = currentTheme then "✓" else "")
                        ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let ThemeDropdown (key, theme, translations: AppTranslations, onClick) =
    if translations.IsEmpty then
        Html.none
    else
        Daisy.dropdown [
            dropdown.hover
            dropdown.end'
            prop.key $"%s{key}-dropdown"
            prop.className "flex-none"
            prop.children [
                Daisy.button.button [
                    button.ghost
                    prop.key "theme-button"
                    prop.text "🌗"
                ]
                Daisy.dropdownContent [
                    prop.key "theme-dropdown-content"
                    prop.className "p-2 shadow menu bg-base-100 rounded-box"
                    prop.tabIndex 0
                    prop.children [
                        Html.ul [
                            prop.key "theme-dropdown-list"
                            prop.children [
                                let themeMenu = ThemeMenu(theme, onClick)

                                themeMenu.group ThemeGroup.Light translations.Home.ThemeGroup.Light
                                themeMenu.item Theme.Light (Emoji "🌞") translations.Home.Theme.Light
                                themeMenu.item Theme.Corporate (Emoji "🏢") translations.Home.Theme.Corporate

                                themeMenu.group ThemeGroup.Dark translations.Home.ThemeGroup.Dark
                                themeMenu.item Theme.Dark (Emoji "🌜") translations.Home.Theme.Dark
                                themeMenu.item Theme.Business (Emoji "💼") translations.Home.Theme.Business
                            ]
                        ]
                    ]
                ]
            ]
        ]