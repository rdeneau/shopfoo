namespace Shopfoo.Client.Tests

open Shopfoo.Client.Filters
open Shopfoo.Client.Pages.Product.Index.Page
open Shopfoo.Client.Pages.Shared
open Shopfoo.Client.Remoting
open Shopfoo.Client.Tests.Types
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core

module private IndexPageTestHelpers =
    /// Minimal env implementing IFullContext + IFillTranslations for testing
    type TestEnv(fullContext: FullContext) =
        interface Env.IFullContext with
            member _.FullContext = fullContext

        interface Env.IFillTranslations with
            member _.FillTranslations _ = ()

    let translations = Translations.In Lang.English
    let env = TestEnv(FullContext.Default.WithTranslations(translations))

    let singleAuthor olid name = Set [ { OLID = OLID olid; Name = name } ]

    let searchedBook olid title authors : SearchedBook = {
        EditionKey = OLID olid
        Title = title
        Subtitle = ""
        Authors = authors
    }

open IndexPageTestHelpers

type IndexPageShould() =
    [<Test>]
    member _.``filter searched books matching the search term "Tidy First"``() =
        // Arrange
        let filters = {
            Filters.defaults with
                CategoryFilters = Some(CategoryFilters.Books(authorId = None, tag = None))
                Search.Term = Some "Tidy First"
        }

        let model: Model = { Products = Remote.Empty; SearchedBooks = Remote.Loading }

        let response: SearchBooksResponse = {
            TotalCount = 6
            Items = [
                searchedBook "OL50637763M" "Tidy First?" (singleAuthor "OL235459A" "Kent Beck")
                searchedBook "OL27196361M" "The Life-Changing Magic of Tidying Up" (singleAuthor "OL7231857A" "Marie Kondo")
                searchedBook "OL8743829M" "Tidy Up (Small World)" (singleAuthor "OL216512A" "Gwenyth Swain")
                searchedBook "OL42003767M" "My First Toddler Coloring Book : Fun with ..." (singleAuthor "OL11125364A" "Zoe Tidy")
                searchedBook "OL18360013M" "Topics from your tidy box" (singleAuthor "OL5455206A" "Frances Matthews")
            ]
        }

        // Act
        let newModel, _cmd = update env filters (Msg.BooksSearched(Ok response)) model

        // Assert
        let expectedProduct: Product = {
            SKU = (OLID "OL50637763M").AsSKU
            Title = "✨ Tidy First?"
            Description = ""
            Category =
                Category.Books {
                    ISBN = ISBN ""
                    Subtitle = ""
                    Authors = set [ { OLID = OLID "OL235459A"; Name = "Kent Beck" } ]
                    Tags = Set.empty
                }
            ImageUrl = ImageUrl.None
        }

        newModel.SearchedBooks =! Remote.Loaded { Items = [ expectedProduct ]; TotalCount = 2 }