module Shopfoo.Shared.Errors

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations

module internal User =
    let errorDetailLevel user =
        match user with
        | User.LoggedIn(_, claims) when claims |> Map.containsKey Feat.Admin -> ErrorDetailLevel.Admin
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

    member this.Build(message, ?category, ?key, ?detail, ?translations) : ApiError = {
        ErrorCategory = defaultArg category String.empty
        ErrorMessage = message
        ErrorType = errorType
        ErrorKey = key
        ErrorDetail = detail
        Translations = defaultArg translations Translations.Empty
    }

and ApiError = {
    ErrorCategory: string
    ErrorMessage: string
    ErrorType: ErrorType
    ErrorKey: TranslationKey option
    ErrorDetail: ErrorDetail option
    Translations: Translations
} with
    static member FromError(error, level, ?key, ?translations) =
        let errorMessage = ErrorMessage.ofError(error).FullMessage

        let errorCategory =
            match level with
            | ErrorDetailLevel.NoDetail -> String.empty
            | ErrorDetailLevel.Admin -> ErrorCategory.ofError error

        match error with
        | Bug exn -> ApiErrorBuilder.Technical.Build(errorMessage, errorCategory, ?key = key, ?detail = exn.AsErrorDetail(level), ?translations = translations)
        | DataError _
        | GuardClause _
        | OperationNotAllowed _ -> ApiErrorBuilder.Technical.Build(errorMessage, errorCategory, ?key = key, ?translations = translations)
        // For ValidationError, use ApiErrorBuilder.Business.Build...

    static member FromException(FirstException exn, user: User) =
        ApiErrorBuilder.Technical.Build(exn.Message, ?detail = exn.AsErrorDetail(User.errorDetailLevel user))

    static member ForAuthenticationError(authError) =
        let errorMessage =
            match authError with
            | TokenInvalid -> "TokenInvalid"
            | UserUnauthorized -> "UserUnauthorized"

        ApiErrorBuilder.Technical.Build(errorMessage)