module Shopfoo.Client.View

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.Router
open Feliz.UseElmish
open Shopfoo.Client.Components.Lang
open Shopfoo.Client.Components.Theme
open Shopfoo.Client.Components.TimedToast
open Shopfoo.Client.Components.User
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Products
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Errors
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
    LangMenus: LangMenu list
    ChangeLangToast: Lang option
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
        LangMenus = LangMenu.all
        ChangeLangToast = None
    },
    Cmd.batch [ // ↩
        Cmd.navigatePage currentPage
        Cmd.ofMsg (Msg.ThemeChanged defaultTheme)
    ]

let private update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    let updateLangStatus lang status = [
        for menu in model.LangMenus do
            if menu.Lang = lang then
                { menu with Status = status }
            else
                menu
    ]

    match msg with
    | Msg.UrlChanged page -> // ↩
        { model with Page = page }, Cmd.none

    | Msg.ThemeChanged theme ->
        { model with Theme = theme }, // ↩
        Cmd.ofEffect (fun _ -> theme.ApplyOnHtml())

    | Msg.ChangeLang(lang, Start) ->
        { model with LangMenus = updateLangStatus lang Remote.Loading; ChangeLangToast = None },
        Cmd.fetchTranslations ( // ↩
            model.FullContext.PrepareRequest { Lang = lang; PageCodes = model.FullContext.Translations.PopulatedPages }
        )

    | Msg.ChangeLang(lang, Done(Ok data)) ->
        {
            model with
                Model.FullContext.Lang = lang
                Model.FullContext.Translations = AppTranslations().Fill(data.Translations)
                LangMenus = updateLangStatus lang (Remote.Loaded())
                ChangeLangToast = Some lang
        },
        Cmd.none

    | Msg.ChangeLang(lang, Done(Error apiError)) ->
        { model with LangMenus = updateLangStatus lang (Remote.LoadError apiError); ChangeLangToast = Some lang }, Cmd.none

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
                LangDropdown("nav-lang", fullContext.Lang, model.LangMenus, fun lang -> dispatch (Msg.ChangeLang(lang, Start)))

                match fullContext.User with
                | User.Anonymous -> ()
                | User.Authorized(userName, _) -> // ↩
                    UserDropdown("nav-user", userName, translations, (fun () -> dispatch Logout))

                if not translations.IsEmpty then
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
            match model.ChangeLangToast with
            | None -> ()
            | Some lang ->
                let langMenu = // ↩
                    model.LangMenus |> List.find (fun menu -> menu.Lang = lang)

                let timedToast (error: ApiError option) =
                    let alertType, text =
                        match error with
                        | None -> alert.success, translations.Home.ChangeLangSuccess
                        | Some apiError -> alert.error, translations.Home.ChangeLangError(langMenu.Label, apiError.ErrorMessage)

                    TimedToast $"app-toast-{DateTime.Now.Ticks}" (Html.text text) [ alertType ] ignore

                match langMenu.Status with
                | Remote.Empty -> ()
                | Remote.Loading -> ()
                | Remote.Loaded() -> timedToast None
                | Remote.LoadError apiError -> timedToast (Some apiError)
        ]
    ]