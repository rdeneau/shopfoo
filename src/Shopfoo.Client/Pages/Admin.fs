module Shopfoo.Client.Pages.Admin

open System
open Browser.Types
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
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
    | ClearResetStatus

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

    | ResetCache(Done result) -> { model with ResetStatus = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult }, Cmd.none
    | ClearResetStatus -> { model with ResetStatus = Remote.Empty }, Cmd.none

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
            prop.onClick (fun e ->
                closeModal e
                dispatch (ResetCache Start)
            )
            prop.text translations.Home.ConfirmReset
        ]

    let dialogProps = {
        MainButton = Some confirmButton
        Translations = {
            Title = translations.Home.Confirmation
            Close = translations.Home.Cancel
            Message = translations.Home.ConfirmResetCacheMessage
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
                        prop.text $"⚙️ %s{translations.Home.Admin}"
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
                        prop.text $"🗄️ %s{translations.Home.ProductCache}"
                    ]

                    Html.p [
                        prop.key "admin-cache-description"
                        prop.className "mb-4"
                        prop.text translations.Home.ResetProductCacheDisclaimer
                    ]

                    Daisy.button.button [
                        button.error
                        prop.key "admin-reset-button"
                        prop.onClick (fun _ ->
                            showModal ()
                            dispatch ClearResetStatus
                        )
                        prop.children [
                            Html.text translations.Home.ResetProductCache
                            match model.ResetStatus with
                            | Remote.Loading -> Daisy.loading [ loading.spinner; prop.key "admin-reset-spinner" ]
                            | _ -> ()
                        ]
                    ]

                    match model.ResetStatus with
                    | Remote.Loaded resetTime ->
                        Daisy.alert [
                            alert.success
                            prop.key "reset-cache-success"
                            prop.className "mt-4 relative"
                            prop.children [
                                Html.text (translations.Home.ResetProductCacheSuccess resetTime)
                                Daisy.button.button [
                                    button.sm
                                    button.circle
                                    button.ghost
                                    prop.className "absolute right-2 top-2"
                                    prop.text "✕"
                                    prop.onClick (fun _ -> dispatch ClearResetStatus)
                                ]
                            ]
                        ]
                    | Remote.LoadError apiError -> // ↩
                        Alert.apiError "reset-cache-error" apiError fullContext.User
                    | _ -> ()
                ]
            ]

            ModalDialog "admin-reset" modalRef dialogProps closeModal
        ]
    ]