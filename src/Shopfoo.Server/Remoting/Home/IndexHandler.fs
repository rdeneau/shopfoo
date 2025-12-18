namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type IndexHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<unit, HomeIndexResponse>()

    override _.Handle lang request user =
        async {
            let! personas = api.Home.GetPersonas()

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations

            match personas with
            | Ok personas -> return response.Ok { Personas = personas }
            | Error error -> return response.ApiError error
        }