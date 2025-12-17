module Shopfoo.Client.Components.Toast

open System
open Fable.Core
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client

let Timeout = TimeSpan.FromMilliseconds(3000)

[<ReactComponent>]
let Toast key content alertProps onDismiss =
    let isVisible, toggle = React.useState true

    React.useEffectOnce (fun () ->
        let hidingTimeoutId =
            JS.setTimeout // ↩
                (fun _ ->
                    toggle false
                    onDismiss ()
                )
                (int Timeout.TotalMilliseconds)

        { new IDisposable with
            member _.Dispose() = JS.clearTimeout hidingTimeoutId
        }
    )

    if not isVisible then
        Html.none
    else
        Daisy.toast [
            toast.bottom
            toast.end'
            prop.key $"%s{key}-toast"
            prop.child (
                Daisy.alert [
                    prop.key $"%s{key}-toast-alert"
                    yield! alertProps
                    prop.child content
                ]
            )
        ]