module Shopfoo.Server.WebApp

open System
open Fable.Remoting.Giraffe
open Fable.Remoting.Server
open Giraffe
open Giraffe.GoodRead
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Shopfoo.Domain.Types.Errors
open Shopfoo.Shared
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Remoting

let errorHandler (logger: ILogger) (FirstException exn) (routeInfo: RouteInfo<HttpContext>) =
    logger.LogError(exn, $"Error at %s{routeInfo.path} on method %s{routeInfo.methodName}")

    // Propagate to the client the exception (1) that we could have thrown (2)
    // (1) Only its message, in order to prevent technical disclosure
    // (2) Using F# helpers: failwith, invalidArg, invalidOp, nullArg
    match exn with
    | Operators.Failure _
    | :? ArgumentException
    | :? ArgumentNullException
    | :? InvalidOperationException -> // ↩
        Propagate(ApiErrorBuilder.Technical.Build(exn.Message, ?detail = exn.AsErrorDetail()))
    | _ -> Ignore

let apiHttpHandler (api: #Remoting.IApi) logger : HttpHandler =
    Remoting.createApi ()
    |> Remoting.withErrorHandler (errorHandler logger)
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue api
    |> Remoting.buildHttpHandler

let webApp (api: Remoting.RootApi) : HttpHandler =
    choose [
        choose [ // ↩
            Require.services<ILogger> (apiHttpHandler api.Home)
            Require.services<ILogger> (apiHttpHandler api.Product)
        ]
        htmlFile "public/index.html"
    ]