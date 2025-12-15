module Shopfoo.Client.Pages.About

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Client.Routing
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let AboutView (fullContext: FullContext) =
    let translations = fullContext.Translations

    // Navigate to the Login page to load translations
    if translations.IsEmpty then
        Router.navigatePage Page.Login

    Html.section [
        prop.key "about-page"
        prop.children [
            Daisy.breadcrumbs [
                prop.key "about-title"
                prop.child (Html.ul [ Html.li [ prop.key "about-title-text"; prop.text translations.Home.About ] ])
            ]
            Html.div [ // ↩
                prop.key "about-disclaimer"
                prop.text $"👉 %s{translations.About.Disclaimer}"
            ]
        ]
    ]