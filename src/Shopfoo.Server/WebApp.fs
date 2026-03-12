module Shopfoo.Server.WebApp

open System
open System.Threading.Tasks
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

type private WebApp = WebApp

let private errorHandler (logger: ILogger<WebApp>) (FirstException exn) (routeInfo: RouteInfo<HttpContext>) =
    logger.LogError(exn, $"Error at %s{routeInfo.path} on method %s{routeInfo.methodName}")

    // Propagate to the client the exception (1) that we could have thrown (2)
    // (1) Only its message, in order to prevent technical disclosure
    // (2) Using F# helpers: failwith, invalidArg, invalidOp, nullArg
    match exn with
    | Operators.Failure _
    | :? ArgumentException
    | :? ArgumentNullException
    | :? InvalidOperationException -> // ↩
        Propagate(ApiError.Technical(exn.Message, ?detail = exn.AsErrorDetail()))
    | _ -> Ignore

let private areaApiHttpHandlerCore (api: #Remoting.IAreaApi) logger : HttpHandler =
    Remoting.createApi ()
    |> Remoting.withErrorHandler (errorHandler logger)
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue api
    |> Remoting.buildHttpHandler

let private areaApiHttpHandler (api: #Remoting.IAreaApi) = Require.services<ILogger<WebApp>> (areaApiHttpHandlerCore api)

/// SPA fallback: serves public/index.html when it exists (production after dotnet publish),
/// and silently skips when it doesn't (local dev — Vite handles the client on its own port).
/// This also covers SPA deep links (e.g. /catalog/products) so that client-side routing works.
let private spaFallback: HttpHandler =
    fun next ctx ->
        let indexPath = "public/index.html"

        if IO.File.Exists indexPath then
            htmlFile indexPath next ctx
        else
            Task.FromResult None

let webApp (rootApi: Remoting.RootApi) : HttpHandler =
    choose [
        areaApiHttpHandler rootApi.Admin
        areaApiHttpHandler rootApi.Catalog
        areaApiHttpHandler rootApi.Home
        areaApiHttpHandler rootApi.Prices
        spaFallback
    ]