namespace Shopfoo.Server.Remoting.Home

open Shopfoo.Domain.Types.Security
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

            return
                match personas with
                | Ok personas ->
                    response.Ok {
                        Personas = [
                            for name, claims in personas ->
                                {
                                    Name = name
                                    Claims = claims
                                    Token = tokenFor (User.LoggedIn(name, claims))
                                }
                        ]
                    }
                | Error error -> response.ApiError error
        }