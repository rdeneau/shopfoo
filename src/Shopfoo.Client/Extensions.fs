[<AutoOpen>]
module Shopfoo.Client.Extensions

open Fable.Core.JsInterop
open Feliz
open Router

type prop with
    static member inline hrefRouted(p: Page) = [ // ↩
        prop.href (p |> Page.toUrlSegments |> Router.formatPath)
        prop.onClick Router.goToUrl
    ]

    static member inline child(item: ReactElement) =
        let key = System.Guid.NewGuid()
        let wrappingFragment = Fable.React.ReactBindings.React.createElement(Fable.React.ReactBindings.React.Fragment, createObj ["key" ==> string key], [item])
        prop.children [ wrappingFragment ]

type Html with
    static member inline a(text: string, p: Page) =
        Html.a [ // ↩
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

module Cmd =
    open Elmish

    module OfFunc =
        /// Command to evaluate a simple function
        let func (fn: unit -> unit) : Cmd<'msg> =
            let bind dispatch =
                try
                    fn ()
                with x ->
                    ()

            [ bind ]