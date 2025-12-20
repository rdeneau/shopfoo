namespace Shopfoo.Client.Components

open System
open Fable.Core
open Feliz
open Feliz.DaisyUI
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Shared.Errors

[<Erase>]
type Buttons =
    [<ReactComponent>]
    static member SaveButton
        (
            key: string,
            label: string,
            tooltipOk: string,
            tooltipError: ApiError -> string,
            tooltipProps: IReactProperty list,
            saveDate: Remote<DateTime>,
            disabled: bool,
            onClick: unit -> unit
        ) =
        Daisy.button.button [
            button.primary
            prop.className "justify-self-start"
            prop.key $"%s{key}-button"

            prop.children [
                Html.text label

                match saveDate with
                | Remote.Empty -> ()
                | Remote.Loading -> Daisy.loading [ loading.spinner; prop.key $"%s{key}-spinner" ]
                | Remote.LoadError apiError ->
                    Daisy.tooltip [
                        tooltip.text (tooltipError apiError)
                        tooltip.error
                        yield! tooltipProps
                        prop.text "❗"
                        prop.key $"%s{key}-error-tooltip"
                    ]
                | Remote.Loaded dateTime ->
                    Daisy.tooltip [
                        tooltip.text $"%s{tooltipOk} @ %i{dateTime.Hour}:%i{dateTime.Minute}:%i{dateTime.Second}"
                        tooltip.success
                        yield! tooltipProps
                        prop.key $"%s{key}-ok-tooltip"
                        prop.children [
                            Html.span [
                                prop.key $"%s{key}-ok-text"
                                prop.text "✓"
                                prop.className "font-bold text-green-500"
                            ]
                        ]
                    ]
            ]

            if disabled || saveDate = Remote.Loading then
                prop.disabled true
            else
                prop.onClick (fun _ -> onClick ())
        ]