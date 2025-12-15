[<RequireQualifiedAccess>]
module Shopfoo.Server.Feat.Home

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Translations

type GetAllowedTranslationsRequest = {
    lang: Lang
    allowed: PageCode Set
    requested: PageCode Set
}

type Api() =
    let translationsByLang =
        Map [
            Lang.English,
            [
                PageCode.About,
                Map [
                    TagCode "Title", "About"
                    TagCode "Disclaimer",
                    "This application is a demo project showcasing the SAFE functional architecture, " // ↩
                    + "with domain workflows based on pseudo algebraic effects."
                ]
                PageCode.Product, Map []
                PageCode.Login, Map [ TagCode "Title", "Login"; TagCode "SelectDemoUser", "Select a demo user" ]
            ]

            Lang.French,
            [
                PageCode.About,
                Map [
                    TagCode "Title", "À propos"
                    TagCode "Disclaimer",
                    "Cette application est un projet démo illustrant l'architecture 'SAFE functional', " // ↩
                    + "avec des 'domain workflows' basé sur des pseudos effets algébriques."
                ]
                PageCode.Product, Map []
                PageCode.Login, Map [ TagCode "Title", "Connexion"; TagCode "SelectDemoUser", "Sélectionner un utilisateur de démo" ]
            ]
        ]

    member _.GetAllowedTranslations(request: GetAllowedTranslationsRequest) : Async<Result<Translations, Error>> =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency

            let pageCodes = Set.intersect request.allowed request.requested

            let pages =
                translationsByLang[request.lang] // ↩
                |> List.filter (fun (pageCode, _) -> pageCodes |> Set.contains pageCode)

            return Ok { Pages = Map pages }
        }