[<AutoOpen>]
module Shopfoo.Client.UI

open System
open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.Router
open Shopfoo.Client.Routing

type prop with
    static member inline hrefRouted(PageUrl pageUrl) = [
        prop.href (Router.formatPath (pageUrl.Segments, queryString = pageUrl.Query))
        prop.onClick Router.goToUrl
    ]

    static member inline child(item: ReactElement) =
        let key = Guid.NewGuid()

        let wrappingFragment =
            Fable.React.ReactBindings.React.createElement (Fable.React.ReactBindings.React.Fragment, createObj [ "key" ==> string key ], [ item ])

        prop.children [ wrappingFragment ]

type Html with
    static member inline a(text: string, p: Page) =
        Html.a [
            yield! prop.hrefRouted p
            prop.key $"{p.Key}"
            prop.text text
        ]

    static member inline classed fn (cn: string) (elm: ReactElement list) =
        fn [ // ↩
            prop.className cn
            prop.children elm
        ]

    static member inline divClassed (cn: string) (elm: ReactElement list) = // ↩
        Html.classed Html.div cn elm

[<RequireQualifiedAccess>]
module JS =
    let runAfter (delay: TimeSpan) f =
        let milliseconds = delay.TotalMilliseconds |> int |> max 0
        JS.setTimeout f milliseconds |> ignore

[<RequireQualifiedAccess>]
module Cmd =
    let ofMsgDelayed (msg: 'msg, delay: TimeSpan) =
        let effect dispatch =
            JS.runAfter delay (fun () -> dispatch msg)

        Cmd.ofEffect effect