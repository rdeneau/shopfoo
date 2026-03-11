[<RequireQualifiedAccess>]
module Shopfoo.Client.Tests.Scenario

open Elmish

type Step<'model, 'msg> = 'model -> 'msg
type Update<'model, 'msg> = 'msg -> 'model -> 'model * Cmd<'msg>

let rec private processCmd (update: Update<'model, 'msg>) (model: 'model) (cmd: Cmd<'msg>) : 'model =
    let dispatchedMsgs = ResizeArray()
    let dispatch: 'msg -> unit = dispatchedMsgs.Add

    for sub in cmd do
        sub dispatch

    (model, dispatchedMsgs)
    ||> Seq.fold (fun m msg ->
        let m', cmd' = update msg m
        processCmd update m' cmd'
    )

/// Simulates an Elmish loop: applies each step to the current model,
/// then recursively processes all cascading messages from the resulting Cmd.
let run (initialModel: 'model) (update: Update<'model, 'msg>) (steps: Step<'model, 'msg> list) : 'model =
    (initialModel, steps)
    ||> List.fold (fun model step ->
        let msg = step model
        let model', cmd = update msg model
        processCmd update model' cmd
    )