module Shopfoo.Client.View

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.Router
open Feliz.UseElmish
open Shopfoo.Client.Components
open Shopfoo.Client.Components.AppNav
open Shopfoo.Client.Components.Lang
open Shopfoo.Client.Components.Theme
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

[<RequireQualifiedAccess>]
type private Toast =
    | Lang of Lang
    | Product of Product * ApiError option

type private Msg =
    | ChangeLang of Lang * ApiCall<GetTranslationsResponse>
    | FillTranslations of Translations
    | Login of User
    | Logout
    | ThemeChanged of Theme
    | UrlChanged of Page
    | ToastOn of Toast
    | ToastOff

type private Model = {
    Page: Page
    Theme: Theme
    FullContext: FullContext
    LangMenus: LangMenu list
    Toast: Toast option
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
        Toast = None
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
    | Msg.ChangeLang(lang, Start) ->
        { model with LangMenus = updateLangStatus lang Remote.Loading; Toast = None },
        Cmd.fetchTranslations ( // ↩
            model.FullContext.PrepareRequest { Lang = lang; PageCodes = model.FullContext.Translations.PopulatedPages }
        )

    | Msg.ChangeLang(lang, Done(Ok data)) ->
        let fullContext = { model.FullContext with Lang = lang; Translations = AppTranslations().Fill(data.Translations) }
        { model with FullContext = fullContext; LangMenus = updateLangStatus lang (Remote.Loaded()) }, Cmd.ofMsg (Msg.ToastOn(Toast.Lang lang))

    | Msg.ChangeLang(lang, Done(Error apiError)) ->
        { model with LangMenus = updateLangStatus lang (Remote.LoadError apiError) }, Cmd.ofMsg (Msg.ToastOn(Toast.Lang lang))

    | Msg.FillTranslations translations -> // ↩
        { model with FullContext = model.FullContext.FillTranslations(translations) }, Cmd.none

    | Msg.Login user ->
        { model with Model.FullContext.User = user },
        match model.Page with
        | Page.Login -> Cmd.navigatePage Page.ProductIndex
        | _ -> Cmd.none

    | Msg.Logout ->
        { model with Model.FullContext.User = User.Anonymous }, // ↩
        Cmd.navigatePage Page.Login

    | Msg.ThemeChanged theme -> { model with Theme = theme }, Cmd.ofEffect (fun _ -> theme.ApplyOnHtml())
    | Msg.UrlChanged page -> { model with Page = page }, Cmd.none

    | Msg.ToastOn toast -> { model with Toast = Some toast }, Cmd.none
    // `Cmd.ofMsgDelayed (Msg.ToastOff, Toast.Timeout)` is not needed because the ToastOff is done with the Toast onDismiss
    | Msg.ToastOff -> { model with Toast = None }, Cmd.none

[<ReactComponent>]
let AppView () =
    let model, dispatch = React.useElmish (init, update)
    let fullContext = model.FullContext
    let translations = fullContext.Translations

    let navbar =
        AppNavBar "app-nav" model.Page translations [
            ThemeDropdown("nav-theme", model.Theme, translations, dispatch << Msg.ThemeChanged)
            LangDropdown("nav-lang", fullContext.Lang, model.LangMenus, fun lang -> dispatch (Msg.ChangeLang(lang, Start)))

            match fullContext.User with
            | User.Anonymous -> ()
            | User.LoggedIn(userName, _) -> // ↩
                UserDropdown("nav-user", userName, translations, (fun () -> dispatch Logout))
        ]

    let fillTranslations = dispatch << Msg.FillTranslations
    let loginUser = dispatch << Msg.Login

    let onSaveProduct (product, error) =
        dispatch (Msg.ToastOn(Toast.Product(product, error)))

    let page =
        match fullContext.User, model.Page with
        | _, Page.About -> Pages.About.AboutView(fullContext)
        | _, Page.NotFound url -> Pages.NotFound.NotFoundView(fullContext, url)
        | User.Anonymous, _ -> Pages.Login.LoginView(fullContext, fillTranslations, loginUser)
        | User.Authorized _, Page.Home
        | User.Authorized _, Page.Login
        | User.Authorized _, Page.ProductIndex -> Pages.Product.Index.IndexView(fullContext, fillTranslations)
        | User.Authorized _, Page.ProductDetail sku -> Pages.Product.Details.DetailsView(fullContext, SKU sku, fillTranslations, onSaveProduct)

    React.router [
        router.pathMode
        router.onUrlChanged (Page.parseFromUrlSegments >> UrlChanged >> dispatch)
        router.children [
            navbar
            Html.div [
                prop.key "app-content"
                prop.className "px-4 py-2"
                prop.children page
            ]
            match model.Toast with
            | None -> ()
            | Some(Toast.Lang lang) ->
                let langMenu = // ↩
                    model.LangMenus |> List.find (fun menu -> menu.Lang = lang)

                let langToast (error: ApiError option) =
                    let alertType, text =
                        match error with
                        | None -> alert.success, translations.Home.ChangeLangOk
                        | Some err -> alert.error, translations.Home.ChangeLangError(langMenu.Label, err.ErrorMessage)

                    let onDismiss () = dispatch Msg.ToastOff

                    Toast.Toast $"toast-lang-{DateTime.Now.Ticks}" [ alertType ] onDismiss [ // ↩
                        Html.text text
                    ]

                match langMenu.Status with
                | Remote.Empty -> ()
                | Remote.Loading -> ()
                | Remote.Loaded() -> langToast None
                | Remote.LoadError apiError -> langToast (Some apiError)

            | Some(Toast.Product(product, error)) ->
                let alertType, text =
                    match error with
                    | None -> alert.success, translations.Product.SaveOk(product.SKU)
                    | Some err -> alert.error, translations.Product.SaveError(product.SKU, err.ErrorMessage)

                let onDismiss () = dispatch Msg.ToastOff

                Toast.Toast $"toast-product-{DateTime.Now.Ticks}" [ alertType ] onDismiss [ // ↩
                    Html.text text
                ]
        ]
    ]