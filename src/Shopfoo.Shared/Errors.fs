module Shopfoo.Shared.Errors

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations

type ErrorType =
    | Business
    | Technical

[<RequireQualifiedAccess>]
type ErrorDetailLevel =
    | NoDetail
    | Admin

type ErrorDetail = { Exception: string }

module internal User =
    let errorDetailLevel user =
        match user with
        | User.LoggedIn(_, claims) when claims |> Map.containsKey Feat.Admin -> ErrorDetailLevel.Admin
        | _ -> ErrorDetailLevel.NoDetail

type System.Exception with
    member this.AsErrorDetail(?level) =
        match level with
        | Some ErrorDetailLevel.Admin ->
#if FABLE_COMPILER
            Some { Exception = $"%A{this}" }
#else
            Some { Exception = $"%s{this.GetType().FullName} %s{this.StackTrace}" }
#endif
        | _ -> None

type ApiError = {
    ErrorCategory: string
    ErrorMessage: string
    ErrorType: ErrorType
    ErrorKey: TranslationKey option
    ErrorDetail: ErrorDetail option
    Translations: Translations
} with
    static member private create category message errorType key detail translations : ApiError = {
        ErrorCategory = defaultArg category String.empty
        ErrorMessage = message
        ErrorType = errorType
        ErrorKey = key
        ErrorDetail = detail
        Translations = defaultArg translations Translations.Empty
    }

    static member Business(message, ?category, ?key, ?detail, ?translations) =
        ApiError.create category message ErrorType.Business key detail translations

    static member Technical(message, ?category, ?key, ?detail, ?translations) =
        ApiError.create category message ErrorType.Technical key detail translations

    static member FromError(error, level, ?key, ?translations) =
        let errorMessage = ErrorMessage.ofError(error).FullMessage

        let errorCategory =
            match level with
            | ErrorDetailLevel.NoDetail -> String.empty
            | ErrorDetailLevel.Admin -> ErrorCategory.ofError error

        match error with
        | BusinessError _ -> ApiError.Business(errorMessage, errorCategory, ?key = key, ?translations = translations)
        | Bug exn -> ApiError.Technical(errorMessage, errorCategory, ?key = key, ?detail = exn.AsErrorDetail(level), ?translations = translations)
        | DataError _
        | OperationNotAllowed _
        | GuardClause _ -> ApiError.Business(errorMessage, errorCategory, ?key = key, ?translations = translations)
        | Validation _ -> ApiError.Business(errorMessage, errorCategory, ?key = key, ?translations = translations)
        | WorkflowError _ -> ApiError.Technical(errorMessage, errorCategory, ?key = key, ?translations = translations)

    static member FromException(FirstException exn, user: User) =
        ApiError.Technical(exn.Message, ?detail = exn.AsErrorDetail(User.errorDetailLevel user))

    static member ForAuthenticationError authError =
        match authError with
        | TokenInvalid -> ApiError.Technical "TokenInvalid"
        | UserUnauthorized -> ApiError.Technical "UserUnauthorized"