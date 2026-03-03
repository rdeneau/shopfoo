namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting

[<Sealed>]
type IndexHandler(api: FeatApi, authorizedPageCodes) =
    inherit SecureQueryDataAndTranslationsHandler<unit, HomeIndexResponse>()

    let buildToken user = user |> JsonFSharp.serialize |> AuthToken

    override _.Handle lang request user =
        async {
            let! personaUsers = api.Home.GetPersonaUsers()

            let! translations =
                api.Home.GetAllowedTranslations {
                    lang = lang
                    allowed = authorizedPageCodes
                    requested = request.TranslationPages
                }

            let response = ResponseBuilder.withTranslations user translations

            return
                match personaUsers with
                | Ok users -> response.Ok { Personas = [ for user in users -> { User = user; Token = buildToken user } ] }
                | Error error -> response.ApiError error
        }