module Shopfoo.Client.Pages.Login

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared
open Shopfoo.Shared.Remoting

type Model = { DemoUsers: Remote<User list> }

type Msg = // ↩
    | HomeDataFetched of ApiResult<HomeIndexResponse * Translations>
    | Login of User

[<RequireQualifiedAccess>]
module private Cmd =
    let loadHomeData (cmder: Cmder, request) =
        cmder.ofApiCall {
            Call = fun api -> api.Home.Index request
            Feat = Feat.Home
            Error = Error >> HomeDataFetched
            Success = Ok >> HomeDataFetched
        }

let private init (fullContext: FullContext) =
    { DemoUsers = Remote.Loading }, // ↩
    Cmd.loadHomeData (fullContext.PrepareQueryWithTranslations())

let private update (fullContext: State<FullContext>) (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | Msg.HomeDataFetched(Ok(data, translations)) ->
        { model with DemoUsers = Remote.Body data.DemoUsers }, // ↩
        Cmd.ofEffect (fun _ -> fullContext.update _.FillTranslations(translations))

    | Msg.HomeDataFetched(Error apiError) ->
        { model with DemoUsers = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fullContext.update _.FillTranslations(apiError.Translations))

    | Msg.Login user ->
        model,
        Cmd.batch [ // ↩
            Cmd.ofEffect (fun _ -> fullContext.update (fun x -> { x with User = user }))
            Cmd.navigatePage Page.ProductIndex
        ]

[<ReactComponent>]
let LoginView () =
    let fullContext = ReactContexts.FullContext.Use()
    let translations = fullContext.current.Translations

    let model, dispatch =
        React.useElmish (init fullContext.current, update fullContext, dependencies = [||])

    Daisy.fieldset [
        prop.key "login-fieldset"
        prop.className "bg-base-200 border border-base-300 rounded-box p-4"
        prop.children [
            Html.legend [ prop.key "login-legend"; prop.text "Login" ]

            Daisy.fieldsetLabel [ prop.key "users-label"; prop.text translations.Login.SelectDemoUser ]
            match model.DemoUsers with
            | Remote.Empty -> ()
            | Remote.Loading ->
                Daisy.skeleton [ // ↩
                    prop.className "h-4 w-28"
                    prop.key "login-skeleton"
                ]

            | Remote.LoadError apiError ->
                Daisy.alert [ // ↩
                    alert.error
                    prop.key "load-error"
                    prop.text apiError.ErrorMessage
                ]

            | Remote.Body users ->
                let authUsers =
                    Map [
                        for user in users do
                            match user with
                            | User.Anonymous -> ()
                            | User.Authorized(userName, _) -> userName, user
                    ]

                Daisy.select [ // ↩
                    prop.key "users-select"
                    prop.children [
                        for userName in authUsers.Keys do
                            Html.option userName
                    ]
                    prop.onChange (fun userName -> dispatch (Msg.Login authUsers[userName]))
                ]
        ]
    ]