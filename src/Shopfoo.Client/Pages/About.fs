module Shopfoo.Client.Pages.About

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let AboutView (fullContext: ReactState<FullContext>) =
    let translations = fullContext.Current.Translations

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