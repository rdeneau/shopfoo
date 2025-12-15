// TODO 2. rename Errors and remove AutoOpen
[<AutoOpen>]
module Shopfoo.Shared.Common

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations

module internal User =
    // TODO 3. add Feat.Admin, remove 'feat' param
    let errorDetailLevel feat user =
        match user with
        | User.Authorized(_, claims) when claims |> List.contains (feat, Access.Admin) -> ErrorDetailLevel.Admin
        | _ -> ErrorDetailLevel.NoDetail

type ErrorDetail = { Exception: string }

type System.Exception with
    member this.AsErrorDetail(?level) =
        match level with
        | Some ErrorDetailLevel.Admin -> Some { Exception = $"%A{exn}" }
        | _ -> None

type ApiErrorBuilder private (errorType) =
    static member Business = ApiErrorBuilder(ErrorType.Business)
    static member Technical = ApiErrorBuilder(ErrorType.Technical)

    member this.Build(message, ?key, ?detail, ?translations) : ApiError = {
        ErrorMessage = message
        ErrorKey = key
        ErrorType = errorType
        ErrorDetail = detail
        Translations = defaultArg translations Translations.Empty
    }

and ApiError = {
    ErrorMessage: string
    ErrorType: ErrorType
    ErrorKey: TranslationKey option
    ErrorDetail: ErrorDetail option
    Translations: Translations
} with
    static member FromError(error, level, ?key, ?translations) =
        let errorMessage = ErrorMessage.ofError error level

        match error with
        | DataError _
        | OperationNotAllowed _ -> ApiErrorBuilder.Technical.Build(errorMessage, ?key = key, ?translations = translations)
        | ValidationError _ -> ApiErrorBuilder.Business.Build(errorMessage, ?key = key, ?translations = translations)
        | Bug exn -> ApiErrorBuilder.Technical.Build(errorMessage, ?key = key, ?detail = exn.AsErrorDetail(level), ?translations = translations)

    static member FromException(FirstException exn, feat, user: User) =
        ApiErrorBuilder.Technical.Build(exn.Message, ?detail = exn.AsErrorDetail(User.errorDetailLevel feat user))

    static member ForAuthenticationError(authError) =
        let errorMessage =
            match authError with
            | TokenInvalid -> "TokenInvalid"
            | UserUnauthorized -> "UserUnauthorized"

        ApiErrorBuilder.Technical.Build(errorMessage)

// TODO 1. move to UI/Client
[<RequireQualifiedAccess>]
type Remote<'a> =
    | Empty
    | Loading
    | LoadError of ApiError
    | Loaded of 'a