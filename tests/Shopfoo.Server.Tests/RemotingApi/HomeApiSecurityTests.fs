namespace Shopfoo.Server.Tests.RemotingApi

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open TUnit.Core
open TUnit.FsCheck

type HomeApiSecurityTests() =
    [<Test; FsCheckProperty(MaxTest = 10)>]
    member _.``Index accepts all personas, including Anonymous``(PersonaOrAnonymousToken token) =
        assertAccepted (api.Home.Index(makeQueryWithTranslations () token))

    [<Test; FsCheckProperty(MaxTest = 10)>]
    member _.``GetTranslations accepts all personas``(PersonaOrAnonymousToken token) =
        let body: GetTranslationsRequest = { Lang = Lang.English; PageCodes = Set [ PageCode.Home ] }
        assertAccepted (api.Home.GetTranslations(makeRequest body token))