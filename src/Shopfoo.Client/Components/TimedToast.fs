module Shopfoo.Client.Components.TimedToast

open System
open Fable.Core
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client

let private oneSecond = TimeSpan.FromMilliseconds(1000)
let private duration = 3. * oneSecond

[<ReactComponent>]
let TimedToast key content alertProps onDismiss =
    let isVisible, toggle = React.useState true

    React.useEffectOnce (fun () ->
        let hidingTimeoutId =
            JS.setTimeout // ↩
                (fun _ ->
                    toggle false
                    onDismiss ()
                )
                (int duration.TotalMilliseconds)

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