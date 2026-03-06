module Shopfoo.Client.Tests.Types

open System
open Shopfoo.Client.Filters
open Shopfoo.Client.Routing
open Shopfoo.Client.Search
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations
open Shopfoo.Tests.Common.FsCheckArbs

type BookTag = private {
    Tag: AlphaNumString option
} with
    member this.Value: string option = this.Tag |> Option.map _.Value

type SanitizedCategoryFilters = private {
    CategoryFilters: CategoryFilters
    BookTag: BookTag
} with
    member this.Value: CategoryFilters =
        match this.CategoryFilters with
        | CategoryFilters.Bazaar _ as x -> x
        | CategoryFilters.Books(authorId, _) -> CategoryFilters.Books(authorId, tag = this.BookTag.Value)

type SanitizedFilters = private {
    CategoryFilters: SanitizedCategoryFilters option
    SearchTerm: AlphaNumString option
    SortBy: (Column * SortDirection) option
} with
    member this.Value: Filters =
        match this.CategoryFilters with
        | None -> Filters.defaults
        | Some categoryFilters -> {
            Filters.defaults with
                CategoryFilters = Some categoryFilters.Value
                Search.Term = this.SearchTerm |> Option.map _.Value
                SortBy = this.SortBy
          }

type SanitizedPage = private {
    Page: Page
    Filters: SanitizedFilters
    Url: AlphaNumString
} with
    member this.Value: Page =
        match this.Page with
        | Page.NotFound _ -> Page.NotFound this.Url.Value
        | Page.ProductDetail { Type = SKUType.Unknown } -> Page.NotFound "unknown-sku"
        | Page.ProductIndex _ -> Page.ProductIndex this.Filters.Value
        | page -> page

module RootApiMock =
    let private notImplemented _ = raise (NotImplementedException())

    let NothingImplemented: RootApi = {
        Admin = { ResetProductCache = notImplemented }
        Catalog = {
            GetBooksData = notImplemented
            GetProducts = notImplemented
            GetProduct = notImplemented
            SaveProduct = notImplemented
            AddProduct = notImplemented
            SearchAuthors = notImplemented
            SearchBooks = notImplemented
        }
        Home = { Index = notImplemented; GetTranslations = notImplemented }
        Prices = {
            AdjustStock = notImplemented
            DetermineStock = notImplemented
            GetPrices = notImplemented
            GetPurchasePrices = notImplemented
            GetSalesStats = notImplemented
            InputSale = notImplemented
            SavePrices = notImplemented
            MarkAsSoldOut = notImplemented
            ReceiveSupply = notImplemented
            RemoveListPrice = notImplemented
        }
    }

type FullContext with
    member fullContext.WithTranslations(translations: Translations) : FullContext = {
        fullContext with
            Translations = AppTranslations().Fill(translations)
    }

    member fullContext.WithUnitTestSession(delayedMessageHandling, ?mockedApi) = {
        fullContext with
            UnitTestSession =
                Some { // ↩
                    DelayedMessageHandling = delayedMessageHandling
                    MockedApi = defaultArg mockedApi RootApiMock.NothingImplemented
                }
    }

[<RequireQualifiedAccess>]
module Lang =
    type Enum =
        | English = 'e'
        | French = 'f'

    let (|FromEnum|) =
        function
        | Enum.English -> Lang.English
        | Enum.French -> Lang.French
        | lang -> invalidArg (nameof lang) $"Unsupported language: {lang}"

type LangSet =
    /// All languages for which translations are available in the repository.
    /// => English and French, but not Latin.
    static member val All = Set [ Lang.English; Lang.French ]

type Translations with
    static member AllPages =
        Set [
            PageCode.Home
            PageCode.Login
            PageCode.Product
        ]

    static member In(lang: Lang) : Translations = {
        Lang = lang // ↩
        Pages = Map Shopfoo.Home.Data.Translations.repository[lang]
    }

    member this.For(?pageCode: PageCode) : Translations = // ↩
        match pageCode with
        | None -> this
        | Some pageCode -> { this with Pages = this.Pages |> Map.filter (fun code _ -> code = pageCode) }