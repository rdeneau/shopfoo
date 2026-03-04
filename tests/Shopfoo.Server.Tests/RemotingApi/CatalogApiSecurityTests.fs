namespace Shopfoo.Server.Tests.RemotingApi

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Remoting
open TUnit.Core

type CatalogApiSecurityTests() =
    static member PersonasWithCatalogEditAccepted = [
        PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted

        PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.Guest, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.Sales, ExpectedResult.Rejected
    ]

    static member PersonasWithCatalogViewAccepted = [
        PersonaOrAnonymousEnum.Guest, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Sales, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted

        PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected
    ]

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogViewAccepted)>]
    member _.``GetProducts accepts only personas with Catalog View (all personas not anonymous)``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.GetProducts(makeQueryWithTranslations Provider.OpenLibrary token))

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogViewAccepted)>]
    member _.``GetProduct accepts only personas with Catalog View (all personas not anonymous)``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.GetProduct(makeQueryWithTranslations anySku token))

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogEditAccepted)>]
    member _.``GetBooksData accepts only personas with Catalog Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.GetBooksData(makeRequest () token))

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogEditAccepted)>]
    member _.``SaveProduct accepts only personas with Catalog Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.SaveProduct(makeRequest anyBookProduct token))

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogEditAccepted)>]
    member _.``AddProduct accepts only personas with Catalog Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.AddProduct(makeRequest anyBookProduct token))

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogEditAccepted)>]
    member _.``SearchAuthors accepts only personas with Catalog Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.SearchAuthors(makeRequest { SearchTerm = "test" } token))

    [<Test; MethodDataSource(nameof CatalogApiSecurityTests.PersonasWithCatalogEditAccepted)>]
    member _.``SearchBooks accepts only personas with Catalog Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Catalog.SearchBooks(makeRequest { SearchTerm = "test" } token))