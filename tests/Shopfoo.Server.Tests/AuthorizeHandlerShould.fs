namespace Shopfoo.Server.Tests

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core
open TUnit.FsCheck

type AuthorizeHandlerShould() =
    [<Test; FsCheckProperty(MaxTest = 10)>]
    member _.``reject a forged token (plain JSON, not encrypted)``(persona: Persona) =
        async {
            let user = User.LoggedIn(persona.Name, persona.Claims)

            let echoHandler =
                { new SecureQueryHandler<unit, User>() with
                    override _.Handle _lang _request user = async { return Ok user }
                }

            let forgedToken = AuthToken(JsonFSharp.serialize user)

            let request: Request<unit> = {
                Token = Some forgedToken
                Lang = Lang.English
                Body = ()
            }

            let! result = authorizeHandler (Claims.single Feat.Admin Access.Edit) echoHandler request
            result =! Error(ServerError.AuthError AuthError.TokenInvalid)
        }