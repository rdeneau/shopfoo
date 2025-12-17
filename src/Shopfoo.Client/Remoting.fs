module Shopfoo.Client.Remoting

open Elmish
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Remoting

/// <summary>
/// Helper type alias to use as the content of a <c>Msg</c> indicating the result of a call to the Remoting API,
/// either a success (<c>Result.Ok</c>) containing the <c>'response</c>,
/// or a failure (<c>Result.Error</c>) containing an <c>ApiError</c>.
/// </summary>
type ApiResult<'response> = Result<'response, ApiError>

/// <summary>
/// Helper type to use as the content of a <c>Msg</c> indicating both:
/// <br/> - The <c>Start</c> of a call to the Remoting API.
/// <br/> - When the call is <c>Done</c> and the result is available.
/// </summary>
type ApiCall<'response> =
    | Start
    | Done of ApiResult<'response>

/// <summary>
/// This object contains the 3 functions needed by <c>cmder.ofApiRequest</c>:
/// <br/> - The <c>Call</c> to the Remoting API, returning the <c>'response</c> type.
/// <br/> - The <c>Error</c> handler, mapping the returned <c>ApiError</c> to the relevant <c>Msg</c> for the View.
/// <br/> - The <c>Success</c> handler, wrapping the <c>'response</c> to the relevant <c>Msg</c> for the View.
/// </summary>
type ApiRequestArgs<'response, 'msg> = {
    Call: RootApi -> Async<Response<'response>>
    Error: ApiError -> 'msg
    Success: 'response -> 'msg
}

/// <summary>
/// <c>Cmd</c> builder to use to perform calls to the Remoting API.
/// </summary>
type Cmder = {
    User: User
} with
    /// <summary>
    /// Wraps a call to the Remoting API, offering an object-based syntax
    /// abstracting an Elmish <c>Cmd.OfAsync.either</c>.
    /// </summary>
    member this.ofApiRequest(args: ApiRequestArgs<'response, 'msg>) : Elmish.Cmd<'msg> =
        let api, cmdOfAsyncEither = Server.api, Cmd.OfAsync.either

        let onResponse result =
            match result with
            | Ok response -> args.Success(response)
            | Error(ServerError.ApiError apiError) -> args.Error(apiError)
            | Error(ServerError.AuthError authError) -> args.Error(ApiError.ForAuthenticationError(authError))

        let onException exn =
            args.Error(ApiError.FromException(exn, this.User))

        cmdOfAsyncEither args.Call api onResponse onException

type FullContext with
    member this.Cmder: Cmder = { User = this.User }

    member this.PrepareRequest body =
        let secureRequest: Request<'a> = {
            Token = this.Token
            Lang = this.Lang
            Body = body
        }

        this.Cmder, secureRequest

    member this.PrepareQueryWithTranslations query =
        this.PrepareRequest { Query = query; TranslationPages = this.Translations.EmptyPages }

/// <summary>
/// Helper type to indicate a property in the MVU <c>Model</c>
/// that is obtained through a call to the Remoting API.
/// </summary>
[<RequireQualifiedAccess>]
type Remote<'a> =
    | Empty
    | Loading
    | LoadError of ApiError
    | Loaded of 'a

[<RequireQualifiedAccess>]
module Remote =
    let ofResult (result: Result<'a, _>) =
        match result with
        | Ok value -> Remote.Loaded value
        | Error error -> Remote.LoadError error