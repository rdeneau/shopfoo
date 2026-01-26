namespace Shopfoo.Client.Components

open Browser.Types
open Fable.Core
open Feliz
open Feliz.DaisyUI

[<RequireQualifiedAccess>]
type CheckboxState =
    | Checked
    | NotChecked
    | Indeterminate

    static member CheckedIf condition =
        if condition then
            CheckboxState.Checked
        else
            CheckboxState.NotChecked

    member this.IsChecked' = (this = Checked)

    member this.Toggle() =
        match this with
        | Checked -> CheckboxState.NotChecked
        | NotChecked -> CheckboxState.Checked
        | Indeterminate -> CheckboxState.Checked

[<Erase>]
type Checkbox =
    [<ReactComponent>]
    static member Checkbox(key: string, state: CheckboxState, children: ReactElement list, onCheck: bool -> unit) =
        let checkboxRef = React.useInputRef ()

        let setIndeterminate (input: HTMLInputElement) = // ↩
            input.indeterminate <- (state = CheckboxState.Indeterminate)

        let toggleState () =
            let newState = state.Toggle()
            onCheck newState.IsChecked'

        React.useEffect ((fun () -> checkboxRef.current |> Option.iter setIndeterminate), dependencies = [| state :> obj |])

        Html.label [
            prop.key $"%s{key}-label"
            prop.className "cursor-pointer flex items-center gap-0 p-2 text-sm w-full"
            prop.tabIndex 0
            prop.onMouseDown _.preventDefault() // to make it work inside a dropdown menu
            prop.onClick (fun (ev: MouseEvent) ->
                ev.preventDefault () // Prevent the default label-click behavior
                toggleState ()
            )
            prop.children [
                Daisy.checkbox [
                    prop.className "mr-2"
                    prop.style [ style.custom ("--size", "1.25rem") ] // Replace `checkbox.sm` not working
                    prop.ref checkboxRef
                    prop.key $"%s{key}-checkbox"
                    prop.isChecked state.IsChecked'
                    prop.onCheckedChange (fun _ -> toggleState ())
                    prop.onClick _.stopPropagation() // To avoid the label to intercept the event
                ]
                yield! children
            ]
        ]