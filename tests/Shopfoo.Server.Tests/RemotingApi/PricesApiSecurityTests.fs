namespace Shopfoo.Server.Tests.RemotingApi

open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Shared.Remoting
open TUnit.Core

type PricesApiSecurityTests() =
    static member PersonasWithSalesEditAccepted = [
        PersonaOrAnonymousEnum.Sales, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted

        PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.Guest, ExpectedResult.Rejected
    ]

    static member PersonasWithSalesViewAccepted = [
        PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Sales, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted

        PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.Guest, ExpectedResult.Rejected
    ]

    static member PersonasWithSalesViewAndWarehouseViewAccepted = [
        PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Sales, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted

        PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.Guest, ExpectedResult.Rejected
    ]

    static member PersonasWithWarehouseEditAccepted = [
        PersonaOrAnonymousEnum.Sales, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.ProductManager, ExpectedResult.Accepted
        PersonaOrAnonymousEnum.Administrator, ExpectedResult.Accepted

        PersonaOrAnonymousEnum.Anonymous, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.Guest, ExpectedResult.Rejected
        PersonaOrAnonymousEnum.CatalogEditor, ExpectedResult.Rejected
    ]

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesViewAccepted)>]
    member _.``GetPrices accepts only personas with Sales View``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.GetPrices(makeRequest anySku token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesViewAccepted)>]
    member _.``GetSalesStats accepts only personas with Sales View``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.GetSalesStats(makeRequest anySku token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesEditAccepted)>]
    member _.``InputSale accepts only personas with Sales Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.InputSale(makeRequest anySale token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesEditAccepted)>]
    member _.``SavePrices accepts only personas with Sales Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.SavePrices(makeRequest anyPrices token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesEditAccepted)>]
    member _.``MarkAsSoldOut accepts only personas with Sales Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.MarkAsSoldOut(makeRequest { SKU = anySku } token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesEditAccepted)>]
    member _.``RemoveListPrice accepts only personas with Sales Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.RemoveListPrice(makeRequest { SKU = anySku } token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesViewAndWarehouseViewAccepted)>]
    member _.``AdjustStock accepts only personas with Sales View and Warehouse View``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.AdjustStock(makeRequest { SKU = anySku; Quantity = 3 } token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesViewAndWarehouseViewAccepted)>]
    member _.``DetermineStock accepts only personas with Sales View and Warehouse View``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.DetermineStock(makeRequest anySku token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithSalesViewAndWarehouseViewAccepted)>]
    member _.``GetPurchasePrices accepts only personas with Sales View and Warehouse View``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.GetPurchasePrices(makeRequest anySku token))

    [<Test; MethodDataSource(nameof PricesApiSecurityTests.PersonasWithWarehouseEditAccepted)>]
    member _.``ReceiveSupply accepts only personas with Warehouse Edit``(PersonaOrAnonymousEnumToken token, result) =
        expect result (api.Prices.ReceiveSupply(makeRequest anySupplyInput token))