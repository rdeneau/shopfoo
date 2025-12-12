namespace Shopfoo.Server.Remoting

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting

[<RequireQualifiedAccess>]
module CatalogFeat =
    type Api() =
        member _.GetProducts() : Async<Result<Product list, Error>> =
            async {
                return
                    Ok [
                    // TODO
                    ]
            }

[<RequireQualifiedAccess>]
module HomeFeat =
    type GetAllowedTranslationsRequest = {
        lang: Lang
        allowed: PageCode Set
        requested: PageCode Set
    }

    type Api() =
        member _.GetAllowedTranslations(request: GetAllowedTranslationsRequest) : Async<Result<Translations, Error>> =
            async {
                // TODO
                // let pageCodes =
                //     Set.intersect request.allowed request.requested
                //     |> Set.toList
                return Ok Translations.Empty
            }

type FeatApi(catalog: CatalogFeat.Api, home: HomeFeat.Api) =
    member val Catalog = catalog
    member val Home = home