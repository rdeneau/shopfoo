module Shopfoo.Client.Pages.About

open Feliz

[<ReactComponent>]
let AboutView () =
    Html.div [ // ↩
        prop.key "about-page"
        prop.text "👉 This application is a demo project showcasing the SAFE functional architecture, with domain workflows based on pseudo algebraic effects."
    ]