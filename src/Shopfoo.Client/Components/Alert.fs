[<RequireQualifiedAccess>]
module Shopfoo.Client.Components.Alert

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Common
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Errors

let private textCode key text = // ↩
    Html.code [
        prop.key $"%s{key}-code"
        prop.className "text-sm"
        prop.text $"%s{text}"
    ]

let private errorDetail apiError key user =
    match apiError.ErrorDetail, user with
    | Some errorDetail, UserCanAccess Feat.Admin ->
        let lines = // ↩
            errorDetail.Exception.Split([| '\n' |]) |> Array.toList

        let title, content =
            match apiError.ErrorCategory, lines with
            | String.NotEmpty as title, _ -> textCode key title, Some errorDetail.Exception
            | _, [ singleLine ] -> textCode key singleLine, None
            | _, firstLine :: otherLines -> textCode key firstLine, Some(String.concat "\n" otherLines)
            | _, [] -> Html.none, None

        Daisy.collapse [
            collapse.arrow
            prop.key $"{key}-collapse"
            prop.tabIndex 0
            prop.className "border"
            prop.children [
                Daisy.collapseTitle [
                    prop.key $"{key}-collapse-title"
                    prop.children [ // ↩
                        Html.text "[Admin]"
                        title
                    ]
                ]
                match content with
                | None -> ()
                | Some content ->
                    Daisy.collapseContent [ // ↩
                        prop.key $"{key}-collapse-content"
                        prop.child (Html.p content)
                    ]
            ]
        ]
    | _ -> Html.none

let apiError key (apiError: ApiError) user =
    Daisy.alert [
        alert.error
        prop.key $"%s{key}-alert"
        prop.children [ // ↩
            Html.div [
                prop.key $"%s{key}-alert-content"
                prop.children [
                    Html.div [
                        prop.key $"%s{key}-message"
                        prop.className "pb-2"
                        prop.text apiError.ErrorMessage
                    ]
                    errorDetail apiError key user
                ]
            ]
        ]
    ]