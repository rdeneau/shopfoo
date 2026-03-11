namespace Shopfoo.Client.Tests

open Elmish
open Shopfoo.Client.Pages.Product.Details.CatalogInfo
open Shopfoo.Client.Remoting
open Shopfoo.Client.Tests.Types
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core

module private CatalogInfoTestHelpers =
    let isbn = ISBN "978-0-13-468599-1"

    let emptyBook: Book = {
        ISBN = isbn
        Subtitle = ""
        Authors = Set.empty
        Tags = Set.empty
    }

    let emptyProduct: Product = {
        SKU = isbn.AsSKU
        Title = ""
        Description = ""
        Category = Category.Books emptyBook
        ImageUrl = ImageUrl.None
    }

    let expectedAuthor: BookAuthor = { OLID = OLID "OL235459A"; Name = "Kent Beck" }
    let expectedTag1: BookTag = "Refactoring"
    let expectedTag2: BookTag = "Software Design"

    let expectedBook: Book = {
        ISBN = isbn
        Subtitle = "A Personal Exercise in Empirical Software Design"
        Authors = Set [ expectedAuthor ]
        Tags = Set [ expectedTag1; expectedTag2 ]
    }

    let expectedProduct: Product = {
        SKU = isbn.AsSKU
        Title = "Tidy First?"
        Description = "A guide to tidying code as a preliminary step to making changes."
        Category = Category.Books expectedBook
        ImageUrl = ImageUrl.Valid "https://covers.openlibrary.org/b/isbn/978-0-13-468599-1-L.jpg"
    }

    let mockedApi = { RootApiMock.NothingImplemented with RootApi.Catalog.AddProduct = fun _ -> async { return Ok() } }

    let fullContext =
        FullContext.Default
            .WithTranslations(Translations.In Lang.English)
            .WithPersona({ Persona = Persona.CatalogEditor; Token = AuthToken "test" })
            .WithUnitTestSession(DelayedMessageHandling.Drop, mockedApi)

    let initialModel: Model = {
        Product = Remote.Loaded emptyProduct
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

    /// Run scenario steps through update, processing all cascading messages from Cmds.
    /// Returns the final model and all onSaveProduct callback invocations.
    let runScenario (steps: ScenarioStep list) =
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
            runScenario [
                productStep Msg.changeName expectedProduct.Title
                bookStep Msg.changeBookSubtitle expectedBook.Subtitle
                bookStep Msg.toggleBookAuthor (true, expectedAuthor)

                productStep Msg.changeDescription expectedProduct.Description
                productStep Msg.changeImageUrl expectedProduct.ImageUrl.Url

                bookStep Msg.toggleBookTag (true, expectedTag1)
                bookStep Msg.toggleBookTag (true, expectedTag2)

                step Msg.addProduct
            ]

        model.Product =! Remote.Loaded expectedProduct
        test <@ model.SaveDate |> Remote.isLoaded @>
        let expectedApiError = None
        saveProductCalls =! [ expectedProduct, expectedApiError ]