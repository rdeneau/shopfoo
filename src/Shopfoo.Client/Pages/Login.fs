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
                Daisy.breadcrumbs [ prop.key "login-title"; prop.child (Html.ul [ Html.li [ prop.key "login-title-text"; prop.text "Login" ] ]) ]

                Daisy.alert [
                    alert.error
                    prop.key "load-error"
                    prop.text apiError.ErrorMessage
                ]

            | Remote.Loaded users ->
                let authUsers =
                    Map [
                        for user in users do
                            match user with
                            | User.Anonymous -> ()
                            | User.Authorized(userName, _) -> userName, user
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
                        Daisy.select [
                            prop.key "users-select"
                            prop.children [
                                Html.option [
                                    prop.key "user-action"
                                    prop.disabled true
                                    prop.text translations.Login.SelectDemoUser
                                    prop.value ""
                                ]
                                for userName in authUsers.Keys do
                                    Html.option [
                                        prop.key $"user-%s{userName.ToLowerInvariant()}"
                                        prop.text userName
                                        prop.value userName
                                    ]
                            ]
                            prop.value ""
                            prop.onChange (fun userName -> dispatch (Msg.Login authUsers[userName]))
                        ]
                    ]
                ]
        ]
    ]