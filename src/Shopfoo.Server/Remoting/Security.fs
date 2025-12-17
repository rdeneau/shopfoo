module Shopfoo.Server.Remoting.Security

open System.Text.Json
open System.Text.Json.Serialization
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

[<AbstractClass>]
type SecureRequestHandler<'requestBody, 'response>() =
    abstract member Handle: lang: Lang -> request: 'requestBody -> user: User -> Async<Response<'response>>

[<AutoOpen>]
module SecureRequestHandlerAliases =
    type SecureCommandHandler<'command> = SecureRequestHandler<'command, unit>
    type SecureQueryHandler<'query, 'response> = SecureRequestHandler<'query, 'response>
    type SecureQueryDataAndTranslationsHandler<'query, 'response> = SecureRequestHandler<QueryDataAndTranslations<'query>, 'response * Translations>

module JsonFSharp =
    let private options = JsonFSharpOptions.Default().ToJsonSerializerOptions()

    let deserialize<'T> (json: string) =
        JsonSerializer.Deserialize<'T>(json, options)

    let serialize (x: 'T) =
        JsonSerializer.Serialize<'T>(x, options)

let private checkToken (claims: Claims) (token: AuthToken option) =
    try
        let user =
            match token with
            | None
            | Some(AuthToken String.NullOrWhiteSpace) -> User.Anonymous
            | Some(AuthToken token) -> JsonFSharp.deserialize token

        let userClaims =
            match user with
            | User.Anonymous -> Set.empty
            | User.LoggedIn(_, userClaims) -> userClaims |> Claims.toSet

        let requiredClaims = claims |> Claims.toSet
        let missingClaims = Set.intersect requiredClaims userClaims

        if missingClaims.IsEmpty then
            Result.Ok user
        else
            Result.Error AuthError.UserUnauthorized

    with _ ->
        Result.Error AuthError.TokenInvalid

let authorizeHandler (claims: Claims) (handler: SecureRequestHandler<'requestBody, 'response>) request : Async<Response<'response>> =
    async {
        match checkToken claims request.Token with
        | Error authError -> return Error(ServerError.AuthError authError)
        | Ok authorizedUser -> return! handler.Handle request.Lang request.Body authorizedUser
    }