module Shopfoo.Client.Search

open System.Text.RegularExpressions
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

type CaseMatching =
    | CaseSensitive
    | CaseInsensitive

let CaseSensitiveIf condition = // ↩
    if condition then CaseSensitive else CaseInsensitive

/// Properties displayed in the table and by which products can be filtered, searched, and/or sorted
[<RequireQualifiedAccess>]
type Column =
    | Num
    | SKU
    | Name
    | Description
    | BazaarCategory
    | BookSubtitle
    | BookAuthors
    | BookTags

[<RequireQualifiedAccess>]
type Highlighting =
    | Active
    | None

    static member ActiveIf condition = // ↩
        if condition then Highlighting.Active else Highlighting.None

type SearchConfig = {
    Columns: Set<Column>
    CaseMatching: CaseMatching
    Highlighting: Highlighting
    Term: string option
}

[<RequireQualifiedAccess>]
type SearchTarget =
    | Description of string
    | Title of string
    | SKU of SKU
    | BazaarCategory of BazaarCategory
    | BookSubtitle of string
    | BookAuthor of BookAuthor
    | BookTag of string

type MatchType =
    | TextMatch
    | NoMatch

type MatchText = {
    Index: int
    MatchType: MatchType
    Text: string
}

type SearchTargetResult = {
    Target: SearchTarget
    Text: string
    Matches: MatchText list
    Highlighting: Highlighting
}

[<RequireQualifiedAccess>]
type SearchStatus =
    | NoMatch
    | Matches of Set<Column>

type SearchResult = Map<Column, SearchTargetResult list>

[<RequireQualifiedAccess>]
module SearchResult =
    let build (searchConfig: SearchConfig) f : SearchResult =
        Map [
            for column in searchConfig.Columns do
                column, f column
        ]

    let status (searchResult: SearchResult) : SearchStatus =
        searchResult
        |> Map.toList
        |> List.choose (fun (column, results) ->
            let b = results |> Seq.collect _.Matches |> Seq.exists (fun x -> x.MatchType = TextMatch)
            Option.ofPair (b, column)
        )
        |> function
            | [] -> SearchStatus.NoMatch
            | matches -> SearchStatus.Matches(Set.ofList matches)

/// Object to perform search operations
type Searcher(config: SearchConfig, translations: AppTranslations) =
    let searchRegexOptions caseMatching =
        List.reduce (|||) [
            RegexOptions.Multiline
            if caseMatching = CaseInsensitive then
                RegexOptions.IgnoreCase
        ]

    let computeMatches (text: string) =
        seq {
            match config.Term with
            | Some(String.NotEmpty as term) ->
                let regex = Regex(pattern = Regex.Escape(term), options = searchRegexOptions config.CaseMatching)
                let parts = regex.Split(text)
                let matches = regex.Matches(text) |> Seq.toArray

                for i in 0 .. parts.Length - 1 do
                    match parts[i] with
                    | String.NullOrWhiteSpace -> ()
                    | part -> NoMatch, part

                    if i < matches.Length then
                        TextMatch, matches[i].Value
            | None
            | Some _ -> ()
        }

    let search target text : SearchTargetResult = {
        Target = target
        Text = text
        Highlighting = config.Highlighting
        Matches = [
            for i, (matchType, part) in computeMatches text |> Seq.indexed do
                {
                    Index = i
                    MatchType = matchType
                    Text = part
                }
        ]
    }

    let searchText targetCase text = search (targetCase text) text
    let searchObject targetCase object getText = search (targetCase object) (getText object)

    let searchSKU sku = searchObject SearchTarget.SKU sku _.Value
    let searchTitle title = searchText SearchTarget.Title title
    let searchDescription description = searchText SearchTarget.Description description
    let searchBazaarCategory category = searchObject SearchTarget.BazaarCategory category translations.Product.StoreCategoryOf
    let searchBookSubtitle subtitle = searchText SearchTarget.BookSubtitle subtitle
    let searchBookAuthor author = searchObject SearchTarget.BookAuthor author _.Name
    let searchBookTag tag = searchText SearchTarget.BookTag tag

    member _.Product (product: Product) column : SearchTargetResult list = [
        match column, product.Category with
        | Column.Num, _ -> () // No search on Num column
        | Column.SKU, _ -> searchSKU product.SKU
        | Column.Name, _ -> searchTitle product.Title
        | Column.Description, _ -> searchDescription product.Description

        | Column.BazaarCategory, Category.Bazaar storeProduct -> searchBazaarCategory storeProduct.Category
        | Column.BazaarCategory, Category.Books _ -> ()

        | Column.BookSubtitle, Category.Bazaar _ -> ()
        | Column.BookSubtitle, Category.Books book -> searchBookSubtitle book.Subtitle

        | Column.BookAuthors, Category.Bazaar _ -> ()
        | Column.BookAuthors, Category.Books book ->
            for author in book.Authors do
                searchBookAuthor author

        | Column.BookTags, Category.Bazaar _ -> ()
        | Column.BookTags, Category.Books book ->
            for tag in book.Tags do
                searchBookTag tag
    ]

    member _.Target(target, text) = search target text