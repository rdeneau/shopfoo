module Shopfoo.Client.View

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Router

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

type private State = { // ↩
    Page: Page
    Theme: Theme
}

let private keyOf x = $"{x}".ToLowerInvariant()

let private setupTheme theme =
    Browser.Dom.document.documentElement.setAttribute ("data-theme", keyOf theme)

let private init () =
    let currentPage = Router.currentPath () |> Page.parseFromUrlSegments
    let defaultTheme = Theme.Light

    { Page = currentPage; Theme = defaultTheme },
    Cmd.batch [ // ↩
        Cmd.navigatePage currentPage
        Cmd.ofMsg (ThemeChanged defaultTheme)
    ]

let private update (msg: Msg) (state: State) : State * Cmd<Msg> =
    match msg with
    | UrlChanged page ->
        { state with Page = page }, // ↩
        Cmd.none

    | ThemeChanged theme ->
        { state with Theme = theme }, // ↩
        Cmd.ofEffect (fun _ -> Fable.Core.JS.setTimeout (fun () -> setupTheme theme) 0 |> ignore)

type private ThemeMenu(currentTheme, dispatch) =
    member _.group(themeGroup: ThemeGroup) =
        let key = keyOf themeGroup

        Daisy.menuTitle [ // ↩
            prop.key $"%s{key}-theme-group"
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
                    prop.onClick (fun _ -> dispatch (ThemeChanged theme))
                    prop.children [
                        Html.span [ prop.key $"{key}-theme-emoji"; prop.text emoji ]
                        Html.span [ prop.key $"{key}-theme-text"; prop.text $"{theme}"; prop.custom ("data-theme", key) ]
                        Html.span [ // ↩
                            prop.key $"{key}-theme-tick"
                            prop.className "ml-auto font-bold text-green-500 min-w-[1em]"
                            prop.text (if theme = currentTheme then "✓" else "")
                        ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let AppView () =
    let state, dispatch = React.useElmish (init, update)

    let navigation =
        Daisy.navbar [
            prop.key "app-nav"
            prop.className "bg-base-200 shadow-sm"
            prop.children [
                Html.div [
                    prop.key "nav-index"
                    prop.className "flex-1"
                    prop.children [ // ↩
                        Html.a ("⚙️ Shopfoo", Page.Index)
                    ]
                ]
                Daisy.dropdown [
                    dropdown.hover
                    dropdown.end'
                    prop.key "theme-dropdown"
                    prop.className "flex-none"
                    prop.children [
                        Daisy.button.button [ button.ghost; prop.key "theme-button"; prop.text "🌗" ]
                        Daisy.dropdownContent [
                            prop.className "p-2 shadow menu bg-base-100 rounded-box"
                            prop.tabIndex 0
                            prop.children [
                                Html.ul [
                                    prop.key "theme-dropdown-list"
                                    prop.children [
                                        let themeMenu = ThemeMenu(state.Theme, dispatch)

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
                Html.div [
                    prop.key "nav-about"
                    prop.className "flex-none text-xs mr-2"
                    prop.children [ // ↩
                        Html.a ("About", Page.About)
                    ]
                ]
            ]
        ]

    let page =
        match state.Page with
        | Page.Index -> Pages.Index.IndexView()
        | Page.About -> Html.div [ prop.key "about-page"; prop.text "Shopfoo Client Application - About Page" ]

    React.router [ // ↩
        router.pathMode
        router.onUrlChanged (Page.parseFromUrlSegments >> UrlChanged >> dispatch)
        router.children [ navigation; Html.div [ prop.key "app-content"; prop.className "px-4 py-2"; prop.children page ] ]
    ]