module Shopfoo.Server.Tests.AuthorizeHandlerShould

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core

[<AutoOpen>]
module private Helpers =
    let catalogViewClaims = Claims.single Feat.Catalog Access.View

    let catalogEditor = User.LoggedIn(PersonaName.catalogEditor, Map [ Feat.About, Access.View; Feat.Catalog, Access.Edit ])

    /// Stub handler that echoes back the authorized user
    type EchoUserHandler() =
        inherit SecureQueryHandler<unit, User>()
        override _.Handle _lang _request user = async { return Ok user }

    let echoHandler = EchoUserHandler()

    let tokenFor user = user |> JsonFSharp.serialize |> AuthToken |> Some

    let makeRequest (token: AuthToken option) : Request<unit> = {
        Token = token
        Lang = Lang.English
        Body = ()
    }

type AuthorizeHandlerShould() =
    [<Test>]
    member _.``authorize a logged-in user given a valid token with sufficient claims``() =
        async {
            let request = makeRequest (tokenFor catalogEditor)
            let! result = authorizeHandler catalogViewClaims echoHandler request
            result =! Ok catalogEditor
        }

    [<Test>]
    member _.``reject the request given no token and claims are required``() =
        async {
            let request = makeRequest None
            let! result = authorizeHandler catalogViewClaims echoHandler request
            result =! Error(ServerError.AuthError AuthError.UserUnauthorized)
        }

    [<Test>]
    member _.``reject the request given a user without the required claims``() =
        async {
            let guest = User.LoggedIn(PersonaName.guest, Map [ Feat.About, Access.View; Feat.Catalog, Access.View ])
            let request = makeRequest (tokenFor guest)
            let editClaims = Claims.single Feat.Catalog Access.Edit
            let! result = authorizeHandler editClaims echoHandler request
            result =! Error(ServerError.AuthError AuthError.UserUnauthorized)
        }