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
            {
                Pages =
                    Map [ // ↩
                        PageCode.About, Map []
                        PageCode.Product, Map []
                        PageCode.Login, Map [
                                                TagCode "SelectDemoUser", "Select a demo user"
                                            ]
                    ]
            }

            Lang.French,
            {
                Pages =
                    Map [ // ↩
                        PageCode.About, Map []
                        PageCode.Product, Map []
                        PageCode.Login, Map [
                                                TagCode "SelectDemoUser", "Sélectionner un utilisateur de démo"
                                            ]
                    ]
            }
        ]

    member _.GetAllowedTranslations(request: GetAllowedTranslationsRequest) : Async<Result<Translations, Error>> =
        async {
            // TODO
            // let pageCodes =
            //     Set.intersect request.allowed request.requested
            //     |> Set.toList
            return Ok Translations.Empty
        }