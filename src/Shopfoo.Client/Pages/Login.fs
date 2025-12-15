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

type Msg =
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

let private update (fullContext: ReactState<FullContext>) (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | Msg.HomeDataFetched(Ok(data, translations)) ->
        { model with DemoUsers = Remote.Loaded data.DemoUsers }, Cmd.ofEffect (fun _ -> fullContext.Update _.FillTranslations(translations))

    | Msg.HomeDataFetched(Error apiError) ->
        { model with DemoUsers = Remote.LoadError apiError }, Cmd.ofEffect (fun _ -> fullContext.Update _.FillTranslations(apiError.Translations))

    | Msg.Login user ->
        model, Cmd.batch [ Cmd.ofEffect (fun _ -> fullContext.Update(fun x -> { x with User = user })); Cmd.navigatePage Page.ProductIndex ]

[<ReactComponent>]
let LoginView (fullContext: ReactState<FullContext>) =
    let translations = fullContext.Current.Translations

    let model, dispatch =
        React.useElmish (init fullContext.Current, update fullContext, dependencies = [||])

    Html.section [
        prop.key "login-page"
        prop.children [
            Daisy.breadcrumbs [
                prop.key "login-title"
                prop.child (Html.ul [ Html.li [ prop.key "login-title-text"; prop.text translations.Home.Login ] ])
            ]

            match model.DemoUsers with
            | Remote.Empty -> ()
            | Remote.Loading -> Daisy.skeleton [ prop.className "h-4 w-28"; prop.key "login-skeleton" ]
            | Remote.LoadError apiError ->
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

                Html.div [
                    prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                    prop.child (
                        Daisy.select [
                            prop.key "users-select"
                            prop.children [
                                Html.option [
                                    prop.disabled true
                                    prop.selected true
                                    prop.text translations.Login.SelectDemoUser
                                ]
                                for userName in authUsers.Keys do
                                    Html.option userName
                            ]
                            prop.onChange (fun userName -> dispatch (Msg.Login authUsers[userName]))
                        ]
                    )
                ]
        ]
    ]