module Shopfoo.Client.Pages.Login

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Components.User
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

type Model = { Personas: Remote<User list> }

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
    { Personas = Remote.Loading }, // ↩
    Cmd.loadHomeData (fullContext.PrepareQueryWithTranslations())

let private update fillTranslations loginUser msg (model: Model) =
    match msg with
    | Msg.HomeDataFetched(Ok(data, translations)) ->
        { model with Personas = Remote.Loaded data.Personas }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations translations)

    | Msg.HomeDataFetched(Error apiError) ->
        { model with Personas = Remote.LoadError apiError }, // ↩
        Cmd.ofEffect (fun _ -> fillTranslations apiError.Translations)

    | Msg.Login user -> // ↩
        model, Cmd.ofEffect (fun _ -> loginUser user)

type private Users =
    static member th(key, text, ?icon: ReactElement, ?className) =
        let icon = defaultArg icon Html.none
        let className = defaultArg className ""

        Html.th [
            prop.key $"users-%s{key}-th"
            prop.className className
            prop.children [
                Html.div [
                    prop.key $"users-%s{key}-th-content"
                    prop.className "flex items-center justify-left"
                    prop.children [
                        icon
                        Html.span [
                            prop.key $"users-%s{key}-text"
                            prop.className "ml-2"
                            prop.text $"%s{text}"
                        ]
                    ]
                ]
            ]
        ]

    static member td(key, text, ?icon: ReactElement) =
        let icon = defaultArg icon Html.none

        Html.td [
            prop.key $"users-%s{key}-td"
            prop.children [
                Html.div [
                    prop.key $"users-%s{key}-td-content"
                    prop.className "flex items-center justify-left"
                    prop.children [
                        icon
                        Html.span [
                            prop.key $"users-%s{key}-text"
                            prop.className "ml-2"
                            prop.text $"%s{text}"
                        ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let LoginView (fullContext, fillTranslations, loginUser) =
    let model, dispatch = React.useElmish (init fullContext, update fillTranslations loginUser, dependencies = [||])

    let translations = fullContext.Translations

    Html.section [
        prop.key "login-page"
        prop.children [
            match model.Personas with
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
                            prop.className "text-sm flex items-center"
                            prop.children [
                                icon fa6Solid.userLock
                                Html.span [
                                    prop.key "login-legend-text"
                                    prop.className "ml-2"
                                    prop.text translations.Home.Login
                                ]
                            ]
                        ]

                        Daisy.fieldsetLabel [ prop.key "login-label"; prop.text translations.Login.SelectPersona ]

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
                                                Users.th ("num", " ")
                                                Users.th ("persona", translations.Login.Persona, className = "w-1/6")
                                                Users.th ("feat-about", translations.Login.Feat.About, icon fa6Solid.circleInfo, "w-1/6")
                                                Users.th ("feat-catalog", translations.Login.Feat.Catalog, icon fa6Solid.folderOpen, "w-1/6")
                                                Users.th ("feat-sales", translations.Login.Feat.Sales, icon fa6Solid.cartShopping, "w-1/6")
                                                Users.th ("feat-warehouse", translations.Login.Feat.Warehouse, icon fa6Solid.store, "w-1/6")
                                                Users.th ("feat-admin", translations.Login.Feat.Admin, icon fa6Solid.gear, "w-1/6")
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
                                                    Users.td ($"%i{i}-num", string (i + 1))
                                                    Users.td ($"%i{i}-user", userName, UserIcon userName)
                                                    Users.td ($"%i{i}-feat-about", (accessTo Feat.About))
                                                    Users.td ($"%i{i}-feat-catalog", (accessTo Feat.Catalog))
                                                    Users.td ($"%i{i}-feat-sales", (accessTo Feat.Sales))
                                                    Users.td ($"%i{i}-feat-warehouse", (accessTo Feat.Warehouse))
                                                    Users.td ($"%i{i}-feat-admin", (accessTo Feat.Admin))
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