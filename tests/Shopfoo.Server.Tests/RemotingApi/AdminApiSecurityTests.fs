namespace Shopfoo.Server.Tests.RemotingApi

open Shopfoo.Shared.Remoting
open TUnit.Core

type AdminApiSecurityTests() =
    [<Test>]
    [<Arguments(PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted)>]
    [<Arguments(PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected)>]
    [<Arguments(PersonaOrAnonymousEnum.Guest, ExpectedResult.Rejected)>]
    [<Arguments(PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Rejected)>]
    [<Arguments(PersonaOrAnonymousEnum.Sales, ExpectedResult.Rejected)>]
    [<Arguments(PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Rejected)>]
    member _.``ResetProductCache accepts personas with Admin Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Admin.ResetProductCache(makeRequest () token))