module Shopfoo.Server.Program

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Shopfoo.Server.DependencyInjection

let private configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore

#if DEBUG
    services.AddCors(_.AddDefaultPolicy(fun policy -> policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore))
    |> ignore
#endif

    services.AddRemotingApi() |> ignore

let private configureApp (app: WebApplication) =
    let rootApiBuilder = app.Services.GetRequiredService<Remoting.RootApiBuilder>()

    app
        .UseStaticFiles() // ↩
        .UseGiraffe(WebApp.webApp (rootApiBuilder.Build()))

let private builderOptions = WebApplicationOptions(WebRootPath = "public")
let private builder = WebApplication.CreateBuilder(builderOptions)

builder.Logging.AddSimpleConsole() |> ignore

configureServices builder.Services

let app = builder.Build()
configureApp app
app.Run()