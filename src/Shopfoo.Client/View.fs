module Shopfoo.Client.View

open System
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
    | ThemeChanged of Theme
    | ChangeLang of Lang * ApiCall<GetTranslationsResponse>
    | FillTranslations of Translations
    | Login of User
    | Logout

type private LangModel = {
    Lang: Lang
    Label: string
    Status: Remote<unit>
}

[<AutoOpen>]
module private LangExtensions =
    type Lang with
        member this.ToModel(label) : LangModel = // ↩
            {
                Lang = this
                Label = label
                Status = Remote.Loaded()
            }

type private Model = {
    Page: Page
    Theme: Theme
    FullContext: FullContext
    Langs: LangModel list
}

[<RequireQualifiedAccess>]
module private Cmd =
    let fetchTranslations (cmder: Cmder, request: Request<GetTranslationsRequest>) =
        let lang = request.Body.Lang // Warning: `request.Lang` is the current lang, not the requested one

        cmder.ofApiRequest {
            Call = fun api -> api.Home.GetTranslations request
            Error = fun err -> ChangeLang(lang, Done(Error err))
            Success = fun data -> ChangeLang(lang, Done(Ok data))
        }

let private keyOf x = $"{x}".ToLowerInvariant()

type private Theme with
    member theme.ApplyOnHtml(?delay: TimeSpan) =
        let setTheme () =
            Browser.Dom.document.documentElement.setAttribute ("data-theme", keyOf theme)

        let milliseconds =
            match delay with
            | Some timeSpan when timeSpan.Ticks > 0 -> int timeSpan.TotalMilliseconds
            | _ -> 0

        // Warning: setTheme works only when executed after the JS loop (React constraint?)
        Fable.Core.JS.setTimeout setTheme milliseconds |> ignore

let private init () =
    let currentPage = Router.currentPath () |> Page.parseFromUrlSegments
    let defaultTheme = Theme.Light

    {
        Page = currentPage
        Theme = defaultTheme
        FullContext = FullContext.Default
        Langs = [
            Lang.English.ToModel "🇺🇸 English"
            Lang.French.ToModel "🇫🇷 Français"
            Lang.Latin.ToModel "🚩 Latin"
        ]
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
        Cmd.ofEffect (fun _ -> theme.ApplyOnHtml())

    | Msg.ChangeLang(lang, Start) ->
        let langs = [
            for langModel in model.Langs do
                if langModel.Lang = lang then
                    { langModel with Status = Remote.Loading }
                else
                    langModel
        ]

        { model with Langs = langs },
        Cmd.fetchTranslations (
            model.FullContext.PrepareRequest { // ↩
                Lang = lang
                PageCodes = model.FullContext.Translations.PopulatedPages
            }
        )

    | Msg.ChangeLang(lang, Done(Ok data)) ->
        let translations = AppTranslations().Fill(data.Translations)
        let fullContext = { model.FullContext with Lang = lang; Translations = translations }

        let langs = [
            for langModel in model.Langs do
                if langModel.Lang = lang then
                    { langModel with Status = Remote.Loaded() }
                else
                    langModel
        ]

        { model with FullContext = fullContext; Langs = langs }, Cmd.none // TODO: Toast success

    | Msg.ChangeLang(lang, Done(Error apiError)) ->
        let langs = [
            for langModel in model.Langs do
                if langModel.Lang = lang then
                    { langModel with Status = Remote.LoadError apiError }
                else
                    langModel
        ]

        { model with Langs = langs }, Cmd.none // TODO: Toast error

    | Msg.FillTranslations translations -> // ↩
        { model with FullContext = model.FullContext.FillTranslations(translations) }, Cmd.none

    | Msg.Login user ->
        { model with Model.FullContext.User = user }, // ↩
        Cmd.navigatePage Page.ProductIndex // TODO: [Navigation] Navigate to product/index only if the current page is Login

    | Msg.Logout ->
        { model with Model.FullContext.User = User.Anonymous }, // ↩
        Cmd.navigatePage Page.Login

// TODO: [UI] move each to a dedicated file in the new Components folder
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

    type private CanClick = CanClick of bool

    [<ReactComponent>]
    let LangDropdown (key, currentLang, langs, dispatch) =
        let langItem (lg: LangModel) =
            let key = $"lang-%s{lg.Lang |> Lang.code}"

            let langLi (CanClick canClick) statusElement =
                Html.li [
                    prop.key $"%s{key}-li"
                    prop.children [
                        Html.a [
                            prop.key $"{key}-link"
                            prop.className [
                                "whitespace-nowrap"
                                if not canClick then
                                    "cursor-default"
                                    "opacity-60"
                            ]

                            if canClick then
                                prop.onClick (fun _ -> dispatch (Msg.ChangeLang(lg.Lang, Start)))
                            else
                                prop.ariaDisabled true

                            prop.children [ // ↩
                                Html.span [ prop.key $"{key}-label"; prop.text $"%s{lg.Label}" ]
                                statusElement
                            ]
                        ]
                    ]
                ]

            match lg.Status with
            | Remote.Empty -> Html.none
            | Remote.Loading ->
                Daisy.loading [
                    prop.key $"{key}-spinner"
                    loading.spinner
                    loading.xs
                    color.textInfo
                ]
                |> langLi (CanClick false)

            | Remote.Loaded() ->
                let isCurrentLang = // ↩
                    lg.Lang = currentLang

                Html.span [
                    prop.key $"{key}-status"
                    prop.className "ml-auto font-bold text-green-500 min-w-[1em]"
                    prop.text (if isCurrentLang then "✓" else "")
                ]
                |> langLi (CanClick (not isCurrentLang))

            | Remote.LoadError apiError ->
                Daisy.tooltip [ // ↩
                    tooltip.text $"Error: %s{apiError.ErrorMessage}"
                    prop.key $"{key}-error-tooltip"
                    prop.child (Daisy.status [ status.error; status.xl ])
                ]
                |> langLi (CanClick true)

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
                                for langModel in langs do
                                    langItem langModel
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
                // TODO: [UI] Breadcrumb in the navbar, replacing h1 in each page
                Html.div [
                    prop.key "nav-home"
                    prop.className "flex-1"
                    prop.child (Html.a ("⚙️ Shopfoo", Page.Home))
                ]

                ThemeDropdown("nav-theme", model.Theme, dispatch)
                LangDropdown("nav-lang", fullContext.Lang, model.Langs, dispatch)

                match fullContext.User with
                | User.Anonymous -> ()
                | User.Authorized(userName, _) -> // ↩
                    UserDropdown("nav-user", userName, dispatch, translations)

                // TODO: hide when loading (no translations)
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