module Shopfoo.Client.View

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.Router
open Feliz.UseElmish
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type private ThemeGroup =
    | Light
    | Dark

[<RequireQualifiedAccess>]
type private Theme =
    | Light
    | Dark
    | Corporate
    | Business

type private Msg =
    | UrlChanged of Page
    | LangChanged of Lang
    | ThemeChanged of Theme
    | TranslationsFetched of ApiResult<Translations>
    | FillTranslations of Translations
    | Login of User
    | Logout

type private Model = {
    Page: Page
    Theme: Theme
    FullContext: FullContext
}

[<RequireQualifiedAccess>]
module private Cmd =
    let fetchTranslations (cmder: Cmder, request) =
        cmder.ofApiCall {
            Call = fun api -> api.Home.GetTranslations request
            Feat = Feat.Home
            Error = Error >> TranslationsFetched
            Success = Ok >> TranslationsFetched
        }

let private keyOf x = $"{x}".ToLowerInvariant()

let private setTheme theme =
    Browser.Dom.document.documentElement.setAttribute ("data-theme", keyOf theme)

let private init () =
    let currentPage = Router.currentPath () |> Page.parseFromUrlSegments
    let defaultTheme = Theme.Light

    {
        Page = currentPage
        Theme = defaultTheme
        FullContext = FullContext.Default
    },
    Cmd.batch [ // ↩
        Cmd.navigatePage currentPage
        Cmd.ofMsg (Msg.ThemeChanged defaultTheme)
    ]

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | Msg.UrlChanged page -> // ↩
        { model with Page = page }, Cmd.none

    | Msg.ThemeChanged theme ->
        { model with Theme = theme }, // ↩
        Cmd.ofEffect (fun _ -> Fable.Core.JS.setTimeout (fun () -> setTheme theme) 0 |> ignore)

    | Msg.LangChanged lang ->
        let fullContext = { model.FullContext with Lang = lang }

        { model with FullContext = fullContext }, // ↩
        Cmd.fetchTranslations (fullContext.PrepareRequest fullContext.Translations.PopulatedPages)

    | Msg.TranslationsFetched(Ok translations) -> // ↩
        { model with Model.FullContext.Translations = AppTranslations().Fill(translations) }, Cmd.none

    | Msg.TranslationsFetched(Error apiError) -> // ↩
        { model with Model.FullContext.Translations = AppTranslations().Fill(apiError.Translations) }, Cmd.none

    | Msg.FillTranslations translations -> // ↩
        { model with FullContext = model.FullContext.FillTranslations(translations) }, Cmd.none

    | Msg.Login user ->
        { model with Model.FullContext.User = user }, // ↩
        Cmd.navigatePage Page.ProductIndex

    | Msg.Logout ->
        { model with Model.FullContext.User = User.Anonymous }, // ↩
        Cmd.navigatePage Page.Login

[<AutoOpen>]
module private Components =
    type ThemeMenu(currentTheme, dispatch) =
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
                        prop.onClick (fun _ -> dispatch (Msg.ThemeChanged theme))
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
    let ThemeDropdown (key, theme, dispatch) =
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
                                let themeMenu = ThemeMenu(theme, dispatch)

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

    [<ReactComponent>]
    let LangDropdown (key, currentLang, dispatch) =
        let langItem lang text =
            let key = lang |> Lang.code

            Html.li [
                prop.key $"%s{key}-lang"
                prop.children [
                    Html.a [
                        prop.key $"{key}-lang-link"
                        prop.className "whitespace-nowrap"
                        prop.onClick (fun _ -> dispatch (Msg.LangChanged lang))
                        prop.children [
                            Html.span [ prop.key $"{key}-lang-text"; prop.text $"%s{text}" ]
                            Html.span [
                                prop.key $"{key}-lang-tick"
                                prop.className "ml-auto font-bold text-green-500 min-w-[1em]"
                                prop.text (if lang = currentLang then "✓" else "")
                            ]
                        ]
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
                    prop.key "lang-button"
                    prop.text (currentLang |> Lang.code)
                ]
                Daisy.dropdownContent [
                    prop.key "lang-dropdown-content"
                    prop.className "p-2 shadow menu bg-base-100 rounded-box"
                    prop.tabIndex 0
                    prop.children [
                        Html.ul [
                            prop.key "lang-dropdown-list"
                            prop.children [ // ↩
                                langItem Lang.English "English"
                                langItem Lang.French "Français"
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    let UserDropdown (key, userName: string, dispatch, translations: AppTranslations) =
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
                            prop.children [
                                Html.li [
                                    prop.key "user-logout"
                                    prop.children [
                                        Html.a [
                                            prop.key "user-logout-link"
                                            prop.className "whitespace-nowrap"
                                            prop.onClick (fun _ -> dispatch Msg.Logout)
                                            prop.text translations.Home.Logout
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let AppView () =
    let model, dispatch = React.useElmish (init, update)
    let fullContext = model.FullContext
    let translations = fullContext.Translations

    let navigation =
        Daisy.navbar [
            prop.key "app-nav"
            prop.className "bg-base-200 shadow-sm"
            prop.children [
                Html.div [
                    prop.key "nav-index"
                    prop.className "flex-1"
                    prop.child (Html.a ("⚙️ Shopfoo", Page.Home))
                ]

                ThemeDropdown("nav-theme", model.Theme, dispatch)
                LangDropdown("nav-lang", fullContext.Lang, dispatch)

                match fullContext.User with
                | User.Anonymous -> ()
                | User.Authorized(userName, _) -> // ↩
                    UserDropdown("nav-user", userName, dispatch, translations)

                Daisy.button.button [
                    button.ghost
                    prop.key "nav-about"
                    prop.text translations.Home.About
                    prop.onClick (fun _ -> Router.navigatePage Page.About)
                ]
            ]
        ]

    let page =
        match fullContext.User, model.Page with
        | _, Page.About -> Pages.About.AboutView(fullContext)
        | User.Anonymous, _ -> Pages.Login.LoginView(fullContext, dispatch << Msg.FillTranslations, dispatch << Msg.Login)
        | User.Authorized _, Page.Home
        | User.Authorized _, Page.Login
        | User.Authorized _, Page.ProductIndex -> Pages.Product.Index.IndexView(fullContext, dispatch << Msg.FillTranslations)
        | User.Authorized _, Page.ProductDetail sku -> Pages.Product.Details.DetailsView(fullContext, SKU sku)

    React.router [
        router.pathMode
        router.onUrlChanged (Page.parseFromUrlSegments >> UrlChanged >> dispatch)
        router.children [
            navigation
            Html.div [
                prop.key "app-content"
                prop.className "px-4 py-2"
                prop.children page
            ]
        ]
    ]