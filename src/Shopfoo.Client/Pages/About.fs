module Shopfoo.Client.Pages.About

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Client.Routing
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let AboutView (fullContext: FullContext) =
    let translations = fullContext.Translations

    // ℹ️ For simplicity's sake, translations for this page are retrieved at startup, on the Login page.
    // ⚠️ If this page is refreshed, the translations will no longer be available!
    // 👉 In this case, we force to redirect to the Login page.
    React.useEffectOnce (fun () ->
        if translations.IsEmpty then
            Router.navigatePage Page.Login
    )

    Html.section [
        prop.key "about-page"
        prop.className "text-sm"
        prop.children [
            Daisy.alert [
                prop.key "about-disclaimer"
                prop.className "my-3"
                prop.text $"ℹ️ %s{translations.Home.AboutDisclaimer}"
            ]
            Html.div [
                prop.key "about-package"
                prop.className "flex justify-center gap-2"
                prop.children [
                    Daisy.badge [
                        badge.soft
                        badge.success
                        prop.key "about-version"
                        prop.text $"🏷️ Version %s{Package.version} (%s{Package.releaseDate})"
                    ]
                    Daisy.badge [
                        badge.soft
                        badge.primary
                        prop.key "about-author"
                        prop.text $"🧑‍💻 %s{Package.author}"
                    ]
                    Daisy.badge [
                        badge.soft
                        badge.info
                        prop.key "about-gitbook"
                        prop.children [
                            Html.text "📖"
                            Daisy.link [
                                link.hover
                                prop.key "about-gitbook-link"
                                prop.href $"%s{Package.homepage}"
                                prop.target "_blank"
                                prop.rel "noopener noreferrer"
                                prop.text "GitBook"
                            ]
                        ]
                    ]
                    Daisy.badge [
                        badge.soft
                        badge.neutral
                        prop.key "about-github"
                        prop.children [
                            Html.text "🧬"
                            Daisy.link [
                                link.hover
                                prop.key "about-github-link"
                                prop.href $"%s{Package.repository.url}"
                                prop.target "_blank"
                                prop.rel "noopener noreferrer"
                                prop.text "GitHub"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]