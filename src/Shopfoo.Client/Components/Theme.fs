module Shopfoo.Client.Components.Theme

open System
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client

[<RequireQualifiedAccess>]
type private ThemeGroup =
    | Light
    | Dark

[<RequireQualifiedAccess>]
type Theme =
    | Light
    | Dark
    | Corporate
    | Business

let private keyOf x = $"{x}".ToLowerInvariant()

type Theme with
    member theme.ApplyOnHtml(?delay: TimeSpan) =
        let setTheme () =
            Browser.Dom.document.documentElement.setAttribute ("data-theme", keyOf theme)

        // Warning: setTheme works only when executed after the JS loop (React constraint?)
        setTheme |> JS.runAfter (defaultArg delay TimeSpan.Zero)

type private ThemeMenu(currentTheme, onClick) =
    member _.group(themeGroup: ThemeGroup) =
        Daisy.menuTitle [ // ↩
            prop.key $"%s{(keyOf themeGroup)}-theme-group"
            prop.text $"{themeGroup} Themes"
        ]

    member _.item(theme: Theme, emoji: string) =
        let key = keyOf theme

        Html.li [
            prop.key $"{key}-theme"
            prop.children [
                Html.a [
                    prop.key $"{key}-theme-link"
                    prop.className "whitespace-nowrap"
                    prop.onClick (fun _ -> onClick theme)
                    prop.children [
                        Html.span [ prop.key $"{key}-theme-emoji"; prop.text emoji ]
                        Html.span [
                            prop.key $"{key}-theme-text"
                            prop.text $"{theme}"
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
let ThemeDropdown (key, theme, onClick) =
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

                            themeMenu.group ThemeGroup.Light
                            themeMenu.item (Theme.Light, "🌞")
                            themeMenu.item (Theme.Corporate, "🏢")

                            themeMenu.group ThemeGroup.Dark
                            themeMenu.item (Theme.Dark, "🌜")
                            themeMenu.item (Theme.Business, "💼")
                        ]
                    ]
                ]
            ]
        ]
    ]