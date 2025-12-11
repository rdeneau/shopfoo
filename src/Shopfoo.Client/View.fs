module Shopfoo.Client.View

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Router

type private Msg = UrlChanged of Page

type private State = { Page: Page }

let private init () =
    let nextPage = Router.currentPath () |> Page.parseFromUrlSegments

    { Page = nextPage }, Cmd.navigatePage nextPage

let private update (msg: Msg) (state: State) : State * Cmd<Msg> =
    match msg with
    | UrlChanged page -> { state with Page = page }, Cmd.none

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