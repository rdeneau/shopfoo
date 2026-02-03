[<RequireQualifiedAccess>]
module Shopfoo.Client.Components.Toast

open System
open Fable.Core
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client

[<RequireQualifiedAccess>]
type Dismiss =
    | Auto
    | Manual

let private Timeout = TimeSpan.FromMilliseconds(3000)

[<ReactComponent>]
let Toast key alertProps dismiss onDismiss children =
    let isVisible, toggle = React.useState true

    React.useEffectOnce (fun () ->
        match dismiss with
        | Dismiss.Auto ->
            let hidingTimeoutId =
                JS.setTimeout
                    (fun _ ->
                        toggle false
                        onDismiss ()
                    )
                    (int Timeout.TotalMilliseconds)

            { new IDisposable with
                member _.Dispose() = JS.clearTimeout hidingTimeoutId
            }

        | Dismiss.Manual ->
            { new IDisposable with
                member _.Dispose() = ()
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
                Html.div [
                    prop.key $"%s{key}-toast-container"
                    prop.className "relative"
                    prop.children [
                        Daisy.alert [
                            prop.key $"%s{key}-toast-alert"
                            yield! alertProps
                            if dismiss = Dismiss.Manual then
                                prop.className "pr-12"
                            prop.children (elems = children)
                        ]

                        // Close button for manual dismiss
                        if dismiss = Dismiss.Manual then
                            Daisy.button.button [
                                button.sm
                                button.circle
                                button.ghost
                                prop.className "absolute right-2 top-2"
                                prop.text "✕"
                                prop.onClick (fun _ ->
                                    toggle false
                                    onDismiss ()
                                )
                            ]
                    ]
                ]
            )
        ]