namespace Shopfoo.Home

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Home.Data

type GetAllowedTranslationsRequest = {
    lang: Lang
    allowed: PageCode Set
    requested: PageCode Set
}

[<Interface>]
type IHomeApi =
    abstract member GetAllowedTranslations: request: GetAllowedTranslationsRequest -> Async<Result<Translations, Error>>
    abstract member GetPersonas: unit -> Async<Result<User list, Error>>

type internal Api() =
    interface IHomeApi with
        member _.GetAllowedTranslations(request) =
            async {
                let pageCodes = Set.intersect request.allowed request.requested

                do! Async.Sleep(millisecondsDueTime = 100 + 50 * pageCodes.Count) // Simulate latency

                let pages =
                    if pageCodes.Count = 0 then
                        Some []
                    else
                        Translations.repository
                        |> Map.tryFind request.lang
                        |> Option.map (List.filter (fun (pageCode, _) -> pageCodes |> Set.contains pageCode))

                match pages with
                | Some pages -> return Ok { Pages = Map pages }
                | None -> return Error(DataError(DataRelatedError.DataNotFound(Id = string request.lang, Type = "")))
            }

        member _.GetPersonas() = async { return Ok Users.personas }

module DependencyInjection =
    open Microsoft.Extensions.DependencyInjection

    type IServiceCollection with
        member services.AddHomeApi() = // ↩
            services.AddSingleton<IHomeApi, Api>()