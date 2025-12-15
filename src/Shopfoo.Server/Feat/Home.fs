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
                PageCode.Home,
                Map [
                    TagCode "About", "About"
                    TagCode "Login", "Login"
                    TagCode "Products", "Products"
                ]

                PageCode.About,
                Map [
                    TagCode "Disclaimer",
                    "This application is a demo project showcasing the SAFE functional architecture, "
                    + "with domain workflows based on pseudo algebraic effects."
                ]

                PageCode.Product,
                Map [ // ↩
                    TagCode "Product:SKU", "Product: {0}"
                ]

                PageCode.Login,
                Map [ // ↩
                    TagCode "SelectDemoUser", "Select a demo user"
                ]
            ]

            Lang.French,
            [
                PageCode.Home,
                Map [
                    TagCode "About", "À propos"
                    TagCode "Login", "Connexion"
                    TagCode "Products", "Produits"
                ]

                PageCode.About,
                Map [
                    TagCode "Disclaimer",
                    "Cette application est un projet démo illustrant l'architecture 'SAFE functional', "
                    + "avec des 'domain workflows' basé sur des pseudos effets algébriques."
                ]

                PageCode.Product,
                Map [ // ↩
                    TagCode "Product:SKU", "Produit : {0}"
                ]

                PageCode.Login,
                Map [ // ↩
                    TagCode "SelectDemoUser", "Sélectionner un utilisateur de démo"
                ]
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