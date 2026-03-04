module Shopfoo.Client.Pages.Admin

open System
open Browser.Types
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components.Dialog
open Shopfoo.Client.Pages.Shared
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Shared.Remoting

type private Model = { ShowModal: bool; ResetStatus: Remote<DateTime> }

type private Msg =
    | OpenModal
    | CloseModal
    | ResetCache of ApiCall<unit>

[<RequireQualifiedAccess>]
module private Cmd =
    let resetCache (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Admin.ResetProductCache request
            Error = Error >> Done >> ResetCache
            Success = Ok >> Done >> ResetCache
        }

let private init () = { ShowModal = false; ResetStatus = Remote.Empty }, Cmd.none

let private update (fullContext: FullContext) (msg: Msg) (model: Model) =
    match msg with
    | OpenModal -> { model with ShowModal = true }, Cmd.none
    | CloseModal -> { model with ShowModal = false }, Cmd.none

    | ResetCache Start ->
        { model with ResetStatus = Remote.Loading }, // ↩
        Cmd.resetCache (fullContext.PrepareRequest())

    | ResetCache(Done result) -> // ↩
        { model with ResetStatus = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult }, Cmd.none

[<ReactComponent>]
let AdminView (env: #Env.IFullContext) =
    let fullContext = env.FullContext
    let translations = env.Translations

    // For simplicity, translations for this page are retrieved at startup on the Login page.
    // If this page is refreshed, the translations will no longer be available — so force redirect to Login.
    React.useEffectOnce (fun () ->
        if translations.IsEmpty then
            Router.navigatePage Page.Login
    )

    let model, dispatch = React.useElmish (init, update fullContext, [||])

    let modalRef: IRefValue<HTMLElement option> = React.useElementRef ()

    let updateModal f =
        modalRef.current
        |> Option.iter (
            function
            | :? HTMLDialogElement as dialog -> f dialog
            | _ -> ()
        )

    // Auto-close modal after success with a short delay
    React.useEffect (fun () ->
        match model.ResetStatus with
        | Remote.Loaded _ when model.ShowModal ->
            JS.runAfter
                (TimeSpan.FromMilliseconds 500)
                (fun () ->
                    updateModal _.close()
                    dispatch CloseModal
                )
        | _ -> ()
    )

    let showModal () =
        dispatch OpenModal
        updateModal _.showModal()

    let closeModal (e: MouseEvent) =
        e.preventDefault ()
        updateModal _.close()
        dispatch CloseModal

    let confirmButton =
        Daisy.button.button [
            button.error
            prop.key "admin-reset-confirm-button"
            prop.disabled (model.ResetStatus = Remote.Loading)
            prop.onClick (fun _ -> dispatch (ResetCache Start))
            prop.children [
                Html.text "Confirm reset" // TODO: translations
                match model.ResetStatus with
                | Remote.Loading -> Daisy.loading [ loading.spinner; prop.key "admin-reset-spinner" ]
                | _ -> ()
            ]
        ]

    let dialogProps = {
        MainButton = Some confirmButton
        Translations = {
            // TODO: translations
            Title = "Confirmation"
            Message = "Are you sure you want to reset the product cache? This will affect all users and restore all products to their initial state."
            Close = "Cancel"
        }
    }

    Html.section [
        prop.key "admin-page"
        prop.className "text-sm flex flex-col gap-4"
        prop.children [
            // Section 1: Admin disclaimer
            Daisy.fieldset [
                prop.key "admin-disclaimer-fieldset"
                prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                prop.children [
                    Html.legend [
                        prop.key "admin-disclaimer-legend"
                        prop.className "text-sm"
                        prop.text "⚙️ Admin" // TODO: translations
                    ]
                    Html.p [ prop.key "admin-disclaimer-text"; prop.text translations.Home.AdminDisclaimer ]
                ]
            ]

            // Section 2: Product cache management
            Daisy.fieldset [
                prop.key "admin-cache-fieldset"
                prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                prop.children [
                    Html.legend [
                        prop.key "admin-cache-legend"
                        prop.className "text-sm"
                        prop.text "Product cache" // TODO: translations
                    ]

                    Html.p [
                        prop.key "admin-cache-description"
                        prop.className "mb-4"
                        // TODO: translations ⬇️
                        prop.text "Reset all product caches and repositories to their initial state (seed data). This action affects all users."
                    ]

                    Daisy.button.button [
                        button.error
                        prop.key "admin-reset-button"
                        prop.text "Reset product cache" // TODO: translations
                        prop.onClick (fun _ -> showModal ())
                    ]

                    match model.ResetStatus with
                    | Remote.Loaded dateTime ->
                        Daisy.alert [
                            alert.success
                            prop.key "admin-reset-success"
                            prop.className "mt-4"
                            // TODO: translations ⬇️
                            prop.text $"Product cache reset successfully at %i{dateTime.Hour}:%02i{dateTime.Minute}:%02i{dateTime.Second}."
                        ]
                    | Remote.LoadError apiError ->
                        Daisy.alert [
                            alert.error
                            prop.key "admin-reset-error"
                            prop.className "mt-4"
                            // TODO: translations ⬇️
                            prop.text $"Failed to reset product cache: %s{apiError.ErrorMessage}"
                        ]
                    | _ -> ()
                ]
            ]

            ModalDialog "admin-reset" modalRef dialogProps closeModal
        ]
    ]