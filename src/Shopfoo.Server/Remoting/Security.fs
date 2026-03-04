module Shopfoo.Server.Remoting.Security

open System
open System.Security.Cryptography
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

    let deserialize<'T> (json: string) = JsonSerializer.Deserialize<'T>(json, options)
    let serialize (x: 'T) = JsonSerializer.Serialize<'T>(x, options)

module private Crypto =
    let private key = RandomNumberGenerator.GetBytes(32)

    let encrypt (plainText: string) : string =
        let plainBytes = Text.Encoding.UTF8.GetBytes(plainText)
        let nonce = RandomNumberGenerator.GetBytes(12)
        let tag = Array.zeroCreate<byte> 16
        let cipherBytes = Array.zeroCreate<byte> plainBytes.Length
        use aes = new AesGcm(key, 16)
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag)

        let result =
            Array.concat [|
                nonce
                cipherBytes
                tag
            |]

        Convert.ToBase64String(result)

    let decrypt (cipherText: string) : string option =
        try
            let data = Convert.FromBase64String(cipherText)

            if data.Length < 28 then
                None
            else
                let nonce = data[..11]
                let tag = data[data.Length - 16 ..]
                let cipherBytes = data[12 .. data.Length - 17]
                let plainBytes = Array.zeroCreate<byte> cipherBytes.Length
                use aes = new AesGcm(key, 16)
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes)
                Text.Encoding.UTF8.GetString(plainBytes) |> Some
        with _ ->
            None

let internal tokenFor user = user |> JsonFSharp.serialize |> Crypto.encrypt |> AuthToken

let private checkToken (claims: Claims) (token: AuthToken option) =
    try
        let user =
            match token with
            | None
            | Some(AuthToken String.NullOrWhiteSpace) -> Some User.Anonymous
            | Some(AuthToken token) -> token |> Crypto.decrypt |> Option.map JsonFSharp.deserialize

        match user with
        | None -> Result.Error AuthError.TokenInvalid
        | Some User.Anonymous ->
            let requiredClaims = claims |> Claims.toSet

            if requiredClaims.IsEmpty then
                Result.Ok User.Anonymous
            else
                Result.Error AuthError.UserUnauthorized
        | Some(User.LoggedIn(_, userClaims) as user) ->
            let requiredClaims = claims |> Claims.toSet
            let missingClaims = requiredClaims - (userClaims |> Claims.toSet)

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