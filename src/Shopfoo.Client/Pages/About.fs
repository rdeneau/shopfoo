module Shopfoo.Client.Pages.About

open Feliz
open Shopfoo.Client
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let AboutView (fullContext: ReactState<FullContext>) =
    let translations = fullContext.Current.Translations

    Html.section [
        prop.key "about-page"
        prop.children [
            Html.h1 [ prop.key "about-title"; prop.text translations.About.Title ]
            Html.div [ prop.key "about-disclaimer"; prop.text $"👉 %s{translations.About.Disclaimer}" ]
        ]
    ]