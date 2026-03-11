namespace Shopfoo.Client.Tests.Pages.Product

open System
open Elmish
open Shopfoo.Client.Pages.Product.Details.CatalogInfo
open Shopfoo.Client.Remoting
open Shopfoo.Client.Tests.Pages
open Shopfoo.Client.Tests.Pages.Product.Examples
open Shopfoo.Client.Tests.Types
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core
open TUnit.FsCheck

module CatalogInfoTestHelpers =
    type FakeData = {
        Now: DateTime
        AddProductResponse: Response<unit>
    } with
        member private this.AddProductResult: ApiResult<unit> = this.AddProductResponse |> Response.toApiResult
        member this.AddProductError: ApiError option = this.AddProductResult |> Result.tryGetError

        member this.AddProductDate: Remote<DateTime> =
            match this.AddProductResult with
            | Ok _ -> Remote.Loaded this.Now
            | Error apiError -> Remote.LoadError apiError

    let mockedApi (fake: FakeData) = {
        RootApiMock.NothingImplemented with
            RootApi.Catalog.AddProduct = fun _ -> async { return fake.AddProductResponse }
            RootApi.Catalog.GetBooksData = fun _ -> async { return Ok { Authors = Set.empty; Tags = Set.empty } }
    }

    let fullContext (fake: FakeData) =
        FullContext.Default
            .WithTranslations(Translations.In Lang.English)
            .WithPersona({ Persona = Persona.CatalogEditor; Token = AuthToken "test" })
            .WithUnitTestSession(DelayedMessageHandling.Drop, mockedApi fake, fake.Now)

    let internal emptyModel: Model = {
        Product = Remote.Loading
        BooksData = Remote.Empty
        SaveDate = Remote.Empty
        SearchedAuthors = Remote.Empty
    }

    let internal productOf (model: Model) =
        match model.Product with
        | Remote.Loaded p -> p
        | other -> failwith $"Expected Remote.Loaded, got %A{other}"

    type internal Step = Model -> Msg // Scenario.Step<Model, Msg>

    [<RequireQualifiedAccess>]
    module internal Step =
        let inline private productStep (f: Product -> Msg) : Step = productOf >> f

        let addProduct: Step = productStep Msg.addProduct
        let fetchProduct product : Step = fun _ -> Msg.ProductFetched(Ok({ Product = Some product }, Translations.Empty))

        let changeProduct (f: 'a -> Product -> Msg) (value: 'a) : Step = productStep (f value)
        let changeBook (f: 'a -> Book -> Product -> Msg) (value: 'a) : Step = productStep (fun product -> f value (bookOf product) product)
        let changeBazaar (f: 'a -> BazaarProduct -> Product -> Msg) (value: 'a) = productStep (fun product -> f value (bazaarOf product) product)

    let internal runScenario fakeData (steps: Step list) =
        let saveProductCalls = ResizeArray()
        let onSaveProduct (product, apiError) = saveProductCalls.Add(product, apiError)

        let update': Msg -> Model -> Model * Cmd<Msg> = // ↩
            update ignore onSaveProduct (fullContext fakeData)

        let finalModel = Scenario.run emptyModel update' steps
        finalModel, List.ofSeq saveProductCalls

open CatalogInfoTestHelpers

type CatalogInfoShould() =
    member private _.``add a product and get`` expected fakeData steps =
        let model, saveProductCalls = runScenario fakeData steps

        (model.Product, model.SaveDate, saveProductCalls)
        =! (Remote.Loaded expected, fakeData.AddProductDate, [ expected, fakeData.AddProductError ])

    [<Test; FsCheckProperty(MaxTest = 5)>]
    member this.``add a complete bazaar product, filling in field by field`` fakeData =
        this.``add a product and get`` MensCottonJacket.product fakeData [
            Step.fetchProduct (Empty.bazaarProduct MensCottonJacket.fsid)

            Step.changeBazaar Msg.changeBazaarCategory MensCottonJacket.category
            Step.changeProduct Msg.changeName MensCottonJacket.product.Title
            Step.changeProduct Msg.changeDescription MensCottonJacket.product.Description
            Step.changeProduct Msg.changeImageUrl MensCottonJacket.product.ImageUrl.Url

            Step.addProduct
        ]

    [<Test; FsCheckProperty(MaxTest = 5)>]
    member this.``add a complete book, filling in field by field`` fakeData =
        this.``add a product and get`` TidyFirst.product fakeData [
            Step.fetchProduct (Empty.bookProduct TidyFirst.isbn)

            Step.changeProduct Msg.changeName TidyFirst.product.Title
            Step.changeProduct Msg.changeDescription TidyFirst.product.Description
            Step.changeProduct Msg.changeImageUrl TidyFirst.product.ImageUrl.Url
            Step.changeBook Msg.changeBookSubtitle TidyFirst.subtitle
            Step.changeBook Msg.toggleBookAuthor (true, TidyFirst.author)
            Step.changeBook Msg.toggleBookTag (true, TidyFirst.tag1)
            Step.changeBook Msg.toggleBookTag (true, TidyFirst.tag2)

            Step.addProduct
        ]