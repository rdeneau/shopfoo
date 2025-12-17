module Shopfoo.Client.Pages.Admin

open Feliz
open Shopfoo.Client.Routing
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let AdminView (fullContext: FullContext) =
    let translations = fullContext.Translations

    // ℹ️ For simplicity's sake, translations for this page are retrieved at startup, on the Login page.
    // ⚠️ If this page is refreshed, the translations will no longer be available!
    // 👉 In this case, we force to redirect to the Login page.
    React.useEffectOnce (fun () ->
        if translations.IsEmpty then
            Router.navigatePage Page.Login
    )

    Html.section [
        prop.key "admin-page"
        prop.className "text-sm"
        prop.text $"⚙️ %s{translations.Home.AdminDisclaimer}"
    ]