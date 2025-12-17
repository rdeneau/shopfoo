module Shopfoo.Client.Pages.About

open Feliz
open Shopfoo.Client.Routing
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let AboutView (fullContext: FullContext) =
    let translations = fullContext.Translations
    // ℹ️ For simplicity's sake, translations for this page are retrieved at startup, on the Login page.
    // ⚠️ If this page is refreshed, the translations will no longer be available!
    // 👉 In this case, we force to redirect to the Login page.
    if translations.IsEmpty then
        React.useEffectOnce (fun () -> Router.navigatePage Page.Login)

    Html.section [
        prop.key "about-page"
        prop.className "text-sm"
        prop.text $"ℹ️ %s{translations.About.Disclaimer}"
    ]