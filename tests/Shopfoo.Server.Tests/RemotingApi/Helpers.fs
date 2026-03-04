[<AutoOpen>]
module Shopfoo.Server.Tests.RemotingApi.Helpers

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open NSubstitute
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary
open Shopfoo.Product.Data.Prices
open Shopfoo.Product.Data.Sales
open Shopfoo.Product.Data.Warehouse
open Shopfoo.Program.Runner
open Shopfoo.Program.Tests.Mocks
open Shopfoo.Server.DependencyInjection
open Shopfoo.Server.Remoting
open Shopfoo.Server.Remoting.Security
open Shopfoo.Shared.Remoting
open Swensen.Unquote

/// Build the RootApi via DI, like the server does, with test overrides
let api =
    let emptyConfig = ConfigurationBuilder().Build() :> IConfiguration

    let services =
        ServiceCollection()
            // Production dependencies
            .AddRemotingApi(emptyConfig)
            // Dependencies overrides for testing
            .AddSingleton<IWorkMonitors, WorkMonitorsMock>()
            .AddSingleton<IFakeStoreClient>(Substitute.For<IFakeStoreClient>())
            .AddSingleton<IOpenLibraryClient>(Substitute.For<IOpenLibraryClient>())
            .AddSingleton(BooksRepository.ofList Fakes.allBooks)
            .AddSingleton(PricesRepository.ofList [])
            .AddSingleton(SalesRepository [])
            .AddSingleton(StockEventRepository [])

    let provider = services.BuildServiceProvider()
    provider.GetRequiredService<RootApiBuilder>().Build()

type PersonaOrAnonymous =
    | Persona of Persona
    | Anonymous

type PersonaOrAnonymousEnum =
    | Anonymous = 'a'
    | Guest = 'g'
    | CatalogEditor = 'c'
    | Sales = 's'
    | ProductManager = 'p'
    | Administrator = 'A'

[<AutoOpen>]
module PersonaPatterns =
    let (|PersonaOrAnonymousFromEnum|) =
        function
        | PersonaOrAnonymousEnum.Anonymous -> Anonymous
        | PersonaOrAnonymousEnum.Guest -> Persona Persona.Guest
        | PersonaOrAnonymousEnum.CatalogEditor -> Persona Persona.CatalogEditor
        | PersonaOrAnonymousEnum.Sales -> Persona Persona.Sales
        | PersonaOrAnonymousEnum.ProductManager -> Persona Persona.ProductManager
        | PersonaOrAnonymousEnum.Administrator -> Persona Persona.Administrator
        | x -> invalidArg "persona" $"Invalid persona: {x}"

let (|PersonaToken|) (persona: Persona) = tokenFor (User.LoggedIn(persona.Name, persona.Claims)) |> Some

let (|PersonaOrAnonymousToken|) =
    function
    | Anonymous -> None
    | Persona(PersonaToken token) -> token

let (|PersonaOrAnonymousEnumToken|) =
    function
    | PersonaOrAnonymousFromEnum(PersonaOrAnonymousToken token) -> token

let makeRequest body token : Request<_> = {
    Token = token
    Lang = Lang.English
    Body = body
}

let makeQueryWithTranslations query token = makeRequest { Query = query; TranslationPages = Set.empty } token

let isAuthError result =
    match result with
    | Error(ServerError.AuthError _) -> true
    | _ -> false

/// Assert that the result is NOT an auth error.
/// The handler may throw (e.g. NRE from unmocked dependencies) — that's fine,
/// it means auth passed and the handler was invoked.
let assertAccepted (work: Async<Result<_, ServerError>>) =
    async {
        try
            let! result = work
            test <@ not (isAuthError result) @>
        with _ex ->
            // Handler threw → auth passed, handler was invoked
            ()
    }

/// Assert that the result is an auth error (UserUnauthorized)
let assertRejected (work: Async<Result<_, ServerError>>) =
    async {
        let! result = work
        result =! Error(ServerError.AuthError AuthError.UserUnauthorized)
    }

type ExpectedResult =
    | Accepted = 'a'
    | Rejected = 'r'

let expect expected work =
    match expected with
    | ExpectedResult.Accepted -> assertAccepted work
    | ExpectedResult.Rejected -> assertRejected work
    | _ -> invalidArg (nameof expected) $"Invalid expected result: {expected}"

let anySku = (ISBN "any").AsSKU

let anyBookProduct: Product = {
    SKU = anySku
    Title = "any"
    Description = ""
    Category =
        Category.Books {
            ISBN = ISBN "any"
            Subtitle = ""
            Authors = Set.empty
            Tags = Set.empty
        }
    ImageUrl = ImageUrl.None
}

let anyPrices = Prices.Initial(anySku, Currency.EUR)

let anySale: Sale = {
    SKU = anySku
    Date = DateOnly.MinValue
    Quantity = 1
    Price = Euros 10m
}

let anySupplyInput: ReceiveSupplyInput = {
    SKU = anySku
    Date = DateOnly.MinValue
    Quantity = 1
    PurchasePrice = Euros 5m
}