module Shopfoo.Client.Components.Dialog

open System
open Browser.Types
open Feliz
open Feliz.DaisyUI
open Shopfoo.Shared.Translations

type DialogTranslations = {
    Title: string
    Message: string
    Close: string
}

type DialogProps = {
    Translations: DialogTranslations
    MainButton: ReactElement option
} with
    static member Empty = {
        Translations = {
            Title = String.Empty
            Message = String.Empty
            Close = String.Empty
        }
        MainButton = None
    }

    static member Confirmation(translations: AppTranslations, question, button) = {
        Translations = {
            Title = translations.Home.Confirmation
            Message = question
            Close = translations.Home.Cancel
        }
        MainButton = Some button
    }

    static member Warning(translations: AppTranslations, message) = {
        Translations = {
            Title = translations.Home.Warning
            Message = message
            Close = translations.Home.Close
        }
        MainButton = None
    }

[<ReactComponent>]
let ModalDialog key (ref: IRefValue<HTMLElement option>) (props: DialogProps) closeModal =
    Daisy.modal.dialog [
        prop.key $"%s{key}-dialog"
        prop.ref ref
        prop.children [
            Daisy.modalBox.div [
                prop.key $"%s{key}-dialog-box"
                prop.children [
                    Html.form [
                        prop.key $"%s{key}-dialog-form"
                        prop.children (
                            Daisy.button.button [
                                button.sm
                                button.circle
                                button.ghost
                                prop.className "absolute right-2 top-2"
                                prop.text "✕"
                                prop.onClick closeModal
                            ]
                        )
                    ]
                    Html.h3 [
                        prop.key $"%s{key}-dialog-title"
                        prop.className "font-bold text-lg"
                        prop.text props.Translations.Title
                    ]
                    Html.p [
                        prop.key $"%s{key}-dialog-message"
                        prop.className "py-4"
                        prop.text props.Translations.Message
                    ]
                    Daisy.modalAction [
                        prop.key $"%s{key}-dialog-actions"
                        prop.children [
                            Daisy.button.button [
                                button.secondary
                                button.outline
                                prop.key $"%s{key}-dialog-cancel-button"
                                prop.text props.Translations.Close
                                prop.onClick closeModal
                            ]
                            match props.MainButton with
                            | None -> ()
                            | Some mainButton -> mainButton
                        ]
                    ]
                ]
            ]
            Daisy.modalBackdrop [ prop.key $"%s{key}-dialog-backdrop"; prop.onClick closeModal ]
        ]
    ]