module Shopfoo.Client.Pages.Login

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type Model = { DemoUsers: Remote<User list> }

type Msg =
    | HomeDataFetched of ApiResult<HomeIndexResponse * Translations>
    | Login of User

[<RequireQualifiedAccess>]
module private Cmd =
    let loadHomeData (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Home.Index request
            Error = Error >> HomeDataFetched
            Success = Ok >> HomeDataFetched
        }

let private init (fullContext: FullContext) =
    { DemoUsers = Remote.Loading }, // ↩
    Cmd.loadHomeData (fullContext.PrepareQueryWithTranslations())

let private update fillTranslations loginUser msg (model: Model) =
    match msg with
    | Msg.HomeDataFetched(Ok(data, translations)) ->
        { model with DemoUsers = Remote.Loaded data.DemoUsers }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | Msg.HomeDataFetched(Error apiError) ->
        { model with DemoUsers = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

    | Msg.Login user -> // ↩
        model, Cmd.ofEffect (fun _ -> loginUser user)

[<ReactComponent>]
let LoginView (fullContext, fillTranslations, loginUser) =
    let model, dispatch =
        React.useElmish (init fullContext, update fillTranslations loginUser, dependencies = [||])

    let translations = fullContext.Translations

    Html.section [
        prop.key "login-page"
        prop.children [
            match model.DemoUsers with
            | Remote.Empty -> ()
            | Remote.Loading -> Daisy.skeleton [ prop.className "h-32 w-full"; prop.key "login-skeleton" ]
            | Remote.LoadError apiError ->
                Daisy.alert [
                    alert.error
                    prop.key "load-error"
                    prop.text apiError.ErrorMessage
                ]

            | Remote.Loaded users ->
                let authUsers = [
                    for user in users do
                        match user with
                        | User.Anonymous -> ()
                        | User.LoggedIn(userName, claims) -> user, userName, claims
                ]

                Daisy.fieldset [
                    prop.key "login-fieldset"
                    prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                    prop.children [
                        Html.legend [
                            prop.key "login-legend"
                            prop.className "text-sm"
                            prop.text $"🔐 %s{translations.Home.Login}"
                        ]

                        Daisy.fieldsetLabel [ prop.key "login-label"; prop.text translations.Login.SelectDemoUser ]

                        Daisy.table [
                            prop.key "users-table"
                            prop.className "table-pin-rows w-full"
                            prop.children [
                                Html.thead [
                                    prop.key "users-table-thead"
                                    prop.child (
                                        Html.tr [
                                            color.bgBase300
                                            prop.children [
                                                Html.th [ prop.key "users-th-num"; prop.text " " ]
                                                Html.th [ prop.key "users-th-user"; prop.text translations.Login.User ]
                                                Html.th [ prop.key "users-th-feat-about"; prop.text $"ℹ️ %s{translations.Login.Feat.About}" ]
                                                Html.th [ prop.key "users-th-feat-catalog"; prop.text $"🗂️ %s{translations.Login.Feat.Catalog}" ]
                                                Html.th [ prop.key "users-th-feat-sales"; prop.text $"🛒 %s{translations.Login.Feat.Sales}" ]
                                                Html.th [ prop.key "users-th-feat-warehouse"; prop.text $"🏬 %s{translations.Login.Feat.Warehouse}" ]
                                                Html.th [ prop.key "users-th-feat-admin"; prop.text $"🔑 %s{translations.Login.Feat.Admin}" ]
                                            ]
                                        ]
                                    )
                                ]
                                Html.tbody [
                                    prop.key "users-table-tbody"
                                    prop.children [
                                        for i, (user, userName, claims) in Seq.indexed authUsers do
                                            let accessTo feat =
                                                match claims |> Map.tryFind feat with
                                                | Some Access.Edit -> translations.Login.Access.Edit
                                                | Some Access.View -> translations.Login.Access.View
                                                | None -> ""

                                            Html.tr [
                                                prop.key $"users-tr-%i{i}"
                                                prop.className "hover:bg-accent hover:fg-accent hover:cursor-pointer"
                                                prop.onClick (fun _ -> dispatch (Msg.Login user))
                                                prop.children [
                                                    Html.td [ prop.key $"users-td-%i{i}-num"; prop.text (i + 1) ]
                                                    Html.td [ prop.key $"users-td-%i{i}-user"; prop.text userName ]
                                                    Html.td [ prop.key $"users-td-%i{i}-feat-about"; prop.text (accessTo Feat.About) ]
                                                    Html.td [ prop.key $"users-td-%i{i}-feat-catalog"; prop.text (accessTo Feat.Catalog) ]
                                                    Html.td [ prop.key $"users-td-%i{i}-feat-sales"; prop.text (accessTo Feat.Sales) ]
                                                    Html.td [ prop.key $"users-td-%i{i}-feat-warehouse"; prop.text (accessTo Feat.Warehouse) ]
                                                    Html.td [ prop.key $"users-td-%i{i}-feat-admin"; prop.text (accessTo Feat.Admin) ]
                                                ]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]