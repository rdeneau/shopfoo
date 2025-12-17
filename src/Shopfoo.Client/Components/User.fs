module Shopfoo.Client.Components.User

open Feliz
open Feliz.DaisyUI
open Shopfoo.Shared.Translations

[<ReactComponent>]
let UserDropdown key (userName: string) (translations: AppTranslations) onClick =
    let logoutMenu =
        Html.li [
            prop.key "user-logout"
            prop.children [
                Html.a [
                    prop.key "user-logout-link"
                    prop.className "whitespace-nowrap"
                    prop.onClick (fun _ -> onClick ())
                    prop.text $"🔓  %s{translations.Home.Logout}"
                ]
            ]
        ]

    Daisy.dropdown [
        dropdown.hover
        dropdown.end'
        prop.key $"%s{key}-dropdown"
        prop.className "flex-none"
        prop.children [
            Daisy.button.button [
                button.ghost
                prop.key "user-button"
                prop.text userName
            ]
            Daisy.dropdownContent [
                prop.key "user-dropdown-content"
                prop.className "p-2 shadow menu bg-base-100 rounded-box"
                prop.tabIndex 0
                prop.children [
                    Html.ul [
                        prop.key "user-dropdown-list"
                        prop.children [ // ↩
                            logoutMenu
                        ]
                    ]
                ]
            ]
        ]
    ]