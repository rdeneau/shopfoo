module Shopfoo.Client.Pages.NotFound

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client.Pages.Shared
open Shopfoo.Client.Routing

[<ReactComponent>]
let NotFoundView (env: #Env.IFullContext) url =
    let translations = env.Translations

    // ℹ️ For simplicity's sake, translations for this page are retrieved at startup, on the Login page.
    // ⚠️ If this page is refreshed, the translations will no longer be available!
    // 👉 In this case, we force to redirect to the Login page.
    React.useEffectOnce (fun () ->
        if translations.IsEmpty then
            Router.navigatePage Page.Login
    )

    Daisy.alert [
        alert.error
        prop.key "product-not-found"
        prop.children [
            Html.span [
                prop.key "pnf-icon"
                prop.text "⛓️‍💥"
                prop.className "text-lg mr-1"
            ]
            Html.span [
                prop.key "pnf-content"
                prop.children [
                    Html.span [ prop.key "pnf-text"; prop.text (translations.Home.ErrorNotFound translations.Home.Page) ]
                    Html.code [ prop.key "pnf-sku"; prop.text $" %s{url} " ]
                ]
            ]
        ]
    ]