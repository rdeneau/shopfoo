namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type GetTranslationsHandler(api: FeatApi) =
    inherit SecureQueryHandler<GetTranslationsRequest, GetTranslationsResponse>()

    override _.Handle lang request user =
        async {
            let pageCodes = // ↩
                if request.Lang = lang then Set.empty else request.PageCodes

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = request.Lang
                    allowed = pageCodes
                    requested = pageCodes
                }

            let response = ResponseBuilder.plain user

            match translations with
            | Ok translations -> return response.Ok { Lang = request.Lang; Translations = translations }
            | Error error -> return response.ApiError error
        }