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
    let translationPages = [
        PageCode.About,
        [
            TagCode "Disclaimer",
            "This application is a demo project showcasing the SAFE functional architecture, "
            + "with domain workflows based on pseudo algebraic effects.",
            "Cette application est un projet démo illustrant l'architecture 'SAFE functional', "
            + "avec des 'domain workflows' basé sur des pseudos effets algébriques."
        ]

        PageCode.Home,
        [
            TagCode "About", "About", "À propos"
            TagCode "Login", "Login", "Connexion"
            TagCode "Logout", "Logout", "Se déconnecter"
            TagCode "Products", "Products", "Produits"

            TagCode "ChangeLangError", "Switching to {0} failed: {1}", "Passage de la langue en {0} en erreur : {1}"
            TagCode "ChangeLangSuccess", "Successful switch to English", "Passage de la langue en français réussi"
        ]

        PageCode.Login,
        [
            TagCode "SelectDemoUser", // ↩
            "Select a demo user",
            "Sélectionner un utilisateur de démo"
        ]

        PageCode.Product,
        [
            TagCode "CatalogInfo", "Catalog Info", "Info Catalogue"
            TagCode "Description", "Description", "Description"
            TagCode "ImageUrl", "Image Url", "Url de l'image"
            TagCode "Name", "Name", "Nom"
            TagCode "Save", "Save", "Enregistrer"
        ]
    ]

    let mapTranslationPages f = [
        for pageCode, translations in translationPages do
            pageCode, translations |> List.map f |> Map.ofList
    ]

    let translationsByLang =
        Map [
            Lang.English, mapTranslationPages (fun (tagCode, en, _) -> tagCode, en)
            Lang.French, mapTranslationPages (fun (tagCode, _, fr) -> tagCode, fr)
        ]

    member _.GetAllowedTranslations(request: GetAllowedTranslationsRequest) : Async<Result<Translations, Error>> =
        async {
            let pageCodes = Set.intersect request.allowed request.requested

            do! Async.Sleep(millisecondsDueTime = 100 + 50 * pageCodes.Count) // Simulate latency

            let pages =
                if pageCodes.Count = 0 then
                    Some []
                else
                    translationsByLang
                    |> Map.tryFind request.lang
                    |> Option.map (List.filter (fun (pageCode, _) -> pageCodes |> Set.contains pageCode))

            match pages with
            | Some pages -> return Ok { Pages = Map pages }
            | None -> return Error(DataError(DataRelatedError.DataNotFound(Id = string request.lang, Type = "")))
        }