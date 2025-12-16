[<RequireQualifiedAccess>]
module Shopfoo.Client.Components.Alert

open Feliz
open Feliz.DaisyUI
open Shopfoo.Client
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Errors

let apiError (key: string) (apiError: ApiError) (user: User) =
    let textCode (text: string) = // ↩
        Html.code [
            prop.key $"{key}-code"
            prop.className "text-sm"
            prop.text text
        ]

    let errorDetail =
        match apiError.ErrorDetail, user with
        | Some errorDetail, User.Authorized(_, claims) when claims.ContainsKey Feat.Admin ->
            let lines = // ↩
                errorDetail.Exception.Split([| '\n' |]) |> Array.toList

            let title, content =
                match lines with
                | [ singleLine ] -> textCode singleLine, None
                | firstLine :: otherLines -> textCode firstLine, Some(String.concat "\n" otherLines)
                | [] -> Html.none, None

            Daisy.collapse [
                collapse.arrow
                prop.key $"{key}-collapse"
                prop.tabIndex 0
                prop.className "border"
                prop.children [
                    Daisy.collapseTitle [ Html.text "[Admin]"; title ]
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

    Daisy.alert [
        alert.error
        prop.key key
        prop.children [ Html.text apiError.ErrorMessage; errorDetail ]
    ]