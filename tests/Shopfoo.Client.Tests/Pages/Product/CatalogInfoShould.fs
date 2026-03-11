namespace Shopfoo.Client.Tests.Pages.Product

open Elmish
open Shopfoo.Client.Pages.Product.Details.CatalogInfo
open Shopfoo.Client.Remoting
open Shopfoo.Client.Tests.Pages
open Shopfoo.Client.Tests.Pages.Product.Examples
open Shopfoo.Client.Tests.Types
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core

module private CatalogInfoTestHelpers =
    let mockedApi = { RootApiMock.NothingImplemented with RootApi.Catalog.AddProduct = fun _ -> async { return Ok() } }

    let fullContext =
        FullContext.Default
            .WithTranslations(Translations.In Lang.English)
            .WithPersona({ Persona = Persona.CatalogEditor; Token = AuthToken "test" })
            .WithUnitTestSession(DelayedMessageHandling.Drop, mockedApi)

    let initialModelWith initialProduct : Model = {
        Product = Remote.Loaded initialProduct
        BooksData = Remote.Loaded { Authors = Set.empty; Tags = Set.empty }
        SaveDate = Remote.Empty
        SearchedAuthors = Remote.Empty
    }

    let private productOf (model: Model) =
        match model.Product with
        | Remote.Loaded p -> p
        | other -> failwith $"Expected Remote.Loaded, got %A{other}"

    let private bookOf (product: Product) =
        match product.Category with
        | Category.Books b -> b
        | other -> failwith $"Expected Category.Books, got %A{other}"

    type ScenarioStep = Model -> Msg

    let inline step (productMsg: Product -> Msg) : ScenarioStep = // ↩
        fun model -> productMsg (productOf model)

    let inline productStep (productMsgFn: 'a -> Product -> Msg) (value: 'a) : ScenarioStep = // ↩
        step (productMsgFn value)

    let inline bookStep (bookMsgFn: 'a -> Book -> Product -> Msg) (value: 'a) : ScenarioStep = // ↩
        step (fun product -> bookMsgFn value (bookOf product) product)

    let private bazaarOf (product: Product) =
        match product.Category with
        | Category.Bazaar b -> b
        | other -> failwith $"Expected Category.Bazaar, got %A{other}"

    let inline bazaarStep (bazaarMsgFn: 'a -> BazaarProduct -> Product -> Msg) (value: 'a) : ScenarioStep =
        step (fun product -> bazaarMsgFn value (bazaarOf product) product)

    /// Run scenario steps through update, processing all cascading messages from Cmds.
    /// Returns the final model and all onSaveProduct callback invocations.
    let runScenario initialModel (steps: ScenarioStep list) =
        let saveProductCalls = ResizeArray()
        let onSaveProduct args = saveProductCalls.Add args

        let update': Msg -> Model -> Model * Cmd<Msg> = // ↩
            update ignore onSaveProduct fullContext

        let rec processCmd (model: Model) (cmd: Cmd<Msg>) : Model =
            let dispatchedMsgs = ResizeArray()
            let dispatch msg = dispatchedMsgs.Add msg

            for sub in cmd do
                sub dispatch

            (model, dispatchedMsgs)
            ||> Seq.fold (fun m msg ->
                let m', cmd' = update' msg m
                processCmd m' cmd'
            )

        let finalModel =
            (initialModel, steps)
            ||> List.fold (fun model step ->
                let msg = step model
                let model', cmd = update' msg model
                processCmd model' cmd
            )

        finalModel, List.ofSeq saveProductCalls

open CatalogInfoTestHelpers

type CatalogInfoShould() =
    [<Test>]
    member _.``fill book fields and add product, triggering onSaveProduct callback``() =
        let model, saveProductCalls =
            runScenario (initialModelWith (Empty.bookProduct TidyFirst.isbn)) [
                productStep Msg.changeName TidyFirst.product.Title
                bookStep Msg.changeBookSubtitle TidyFirst.subtitle
                bookStep Msg.toggleBookAuthor (true, TidyFirst.author)

                productStep Msg.changeDescription TidyFirst.product.Description
                productStep Msg.changeImageUrl TidyFirst.product.ImageUrl.Url

                bookStep Msg.toggleBookTag (true, TidyFirst.tag1)
                bookStep Msg.toggleBookTag (true, TidyFirst.tag2)

                step Msg.addProduct
            ]

        model.Product =! Remote.Loaded TidyFirst.product
        test <@ model.SaveDate |> Remote.isLoaded @>
        let expectedApiError = None
        saveProductCalls =! [ TidyFirst.product, expectedApiError ]

    [<Test>]
    member _.``fill bazaar fields and add product, triggering onSaveProduct callback``() =
        let model, saveProductCalls =
            runScenario (initialModelWith (Empty.bazaarProduct MensCottonJacket.fsid)) [
                bazaarStep Msg.changeBazaarCategory MensCottonJacket.category
                productStep Msg.changeName MensCottonJacket.product.Title
                productStep Msg.changeDescription MensCottonJacket.product.Description
                productStep Msg.changeImageUrl MensCottonJacket.product.ImageUrl.Url
                step Msg.addProduct
            ]

        model.Product =! Remote.Loaded MensCottonJacket.product
        test <@ model.SaveDate |> Remote.isLoaded @>
        let expectedApiError = None
        saveProductCalls =! [ MensCottonJacket.product, expectedApiError ]