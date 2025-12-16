module Shopfoo.Client.View

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.Router
open Feliz.UseElmish
open Shopfoo.Client.Components.Lang
open Shopfoo.Client.Components.Theme
open Shopfoo.Client.Components.User
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

type private Msg =
    | UrlChanged of Page
    | ThemeChanged of Theme
    | ChangeLang of Lang * ApiCall<GetTranslationsResponse>
    | FillTranslations of Translations
    | Login of User
    | Logout

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

let private init () =
    let currentPage = Router.currentPath () |> Page.parseFromUrlSegments
    let defaultTheme = Theme.Light

    {
        Page = currentPage
        Theme = defaultTheme
        FullContext = FullContext.Default
        Langs = LangModel.all
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

        { model with FullContext = fullContext; Langs = langs }, Cmd.none // [UI] TODO: Toast success

    | Msg.ChangeLang(lang, Done(Error apiError)) ->
        let langs = [
            for langModel in model.Langs do
                if langModel.Lang = lang then
                    { langModel with Status = Remote.LoadError apiError }
                else
                    langModel
        ]

        { model with Langs = langs }, Cmd.none // TODO: [UI] Toast error

    | Msg.FillTranslations translations -> // ↩
        { model with FullContext = model.FullContext.FillTranslations(translations) }, Cmd.none

    | Msg.Login user ->
        { model with Model.FullContext.User = user }, // ↩
        Cmd.navigatePage Page.ProductIndex // TODO: [Navigation] Navigate to product/index only if the current page is Login

    | Msg.Logout ->
        { model with Model.FullContext.User = User.Anonymous }, // ↩
        Cmd.navigatePage Page.Login

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

                ThemeDropdown("nav-theme", model.Theme, dispatch << Msg.ThemeChanged)
                LangDropdown("nav-lang", fullContext.Lang, model.Langs, fun lang -> dispatch (Msg.ChangeLang(lang, Start)))

                match fullContext.User with
                | User.Anonymous -> ()
                | User.Authorized(userName, _) -> // ↩
                    UserDropdown("nav-user", userName, translations, (fun () -> dispatch Logout))

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