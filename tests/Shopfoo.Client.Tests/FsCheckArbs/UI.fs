namespace Shopfoo.Client.Tests.FsCheckArbs

open FsCheck.FSharp
open Shopfoo.Client.Filters
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types

[<AutoOpen>]
module UIGen =
    let private arbMap =
        ArbMap.defaults // ↩
        |> ArbMap.mergeWith<CommonArbs>
        |> ArbMap.mergeWith<DomainArbs>

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