module Shopfoo.Client.Remoting

open System
open Elmish
open Fable.Core
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared
open Shopfoo.Shared.Remoting

type ApiResult<'response> = Result<'response, ApiError>

type ApiCall<'response> =
    | Start
    | Done of ApiResult<'response>

type ApiCallArgs<'response, 'msg> = {
    Call: RootApi -> Async<Response<'response>>
    Feat: Feat
    Error: ApiError -> 'msg
    Success: 'response -> 'msg
}

type Cmder = {
    User: User
} with
    member this.ofApiCall(args: ApiCallArgs<'response, 'msg>) : Elmish.Cmd<'msg> =
        let api, cmdOfAsyncEither = Server.api, Cmd.OfAsync.either

        let onResponse result =
            match result with
            | Ok response -> args.Success(response)
            | Error(ServerError.ApiError apiError) -> args.Error(apiError)
            | Error(ServerError.AuthError authError) -> args.Error(ApiError.ForAuthenticationError(authError))

        let onException exn =
            args.Error(ApiError.FromException(exn, args.Feat, this.User))

        cmdOfAsyncEither args.Call api onResponse onException

    member this.ofMsgDelayed<'msg>(msg: 'msg, delay: TimeSpan) =
        let milliseconds = int delay.TotalMilliseconds

        let sub dispatch =
            JS.setTimeout (fun () -> dispatch msg) milliseconds |> ignore

        Cmd.ofEffect sub

type FullContext with
    member this.Cmder: Cmder = { User = this.User }

    member this.PrepareRequest body =
        let secureRequest: Request<'a> = {
            Lang = this.Lang
            Token = this.Token
            Body = body
        }

        this.Cmder, secureRequest

    member this.PrepareQueryWithTranslations query =
        this.PrepareRequest { Query = query; TranslationPages = this.Translations.EmptyPages }