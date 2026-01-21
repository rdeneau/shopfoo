module Shopfoo.Client.Routing

open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop
open Shopfoo.Client.Filters
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog

[<AutoOpen>]
module private Keys =
    type CaseMatching with
        member this.Key =
            match this with
            | CaseSensitive -> Some "yes"
            | CaseInsensitive -> None // Default is case-insensitive

    type Highlighting with
        member this.Key =
            match this with
            | Highlighting.None -> Some "no"
            | Highlighting.Active -> None // Default is highlighting active

    type SKU with
        member this.Key =
            match this.Type, this.Value with
            | SKUType.FSID _, (String.StartsWith "FS-" as value) -> value
            | SKUType.FSID _, value -> $"FS-%s{value}"
            | SKUType.ISBN _, value -> $"BN-%s{value}"
            | SKUType.Unknown, _ -> ""

    [<RequireQualifiedAccess>]
    module BazaarCategory =
        let key =
            function
            | BazaarCategory.Clothing -> "clothing"
            | BazaarCategory.Electronics -> "electronics"
            | BazaarCategory.Jewelry -> "jewelry"

        let tryFromKey (key: string) : BazaarCategory option =
            match key.ToLowerInvariant() with
            | "clothing" -> Some BazaarCategory.Clothing
            | "electronics" -> Some BazaarCategory.Electronics
            | "jewelry" -> Some BazaarCategory.Jewelry
            | _ -> None

    [<RequireQualifiedAccess>]
    module Column =
        let key =
            function
            | Column.Num -> "num"
            | Column.SKU -> "sku"
            | Column.Name -> "name"
            | Column.Description -> "description"
            | Column.BazaarCategory -> "category"
            | Column.BookSubtitle -> "subtitle"
            | Column.BookAuthors -> "authors"
            | Column.BookTags -> "tags"

    let (|Col|_|) key : Column option =
        match String.toLower key with
        | "num" -> Some Column.Num
        | "sku" -> Some Column.SKU
        | "name" -> Some Column.Name
        | "description" -> Some Column.Description
        | "category" -> Some Column.BazaarCategory
        | "subtitle" -> Some Column.BookSubtitle
        | "authors" -> Some Column.BookAuthors
        | "tags" -> Some Column.BookTags
        | _ -> None

    [<RequireQualifiedAccess>]
    module SortBy =
        let key =
            function
            | col, Ascending -> col |> Column.key
            | col, Descending -> $"{col |> Column.key}-desc"

    let (|Desc|_|) s =
        match String.toLower s with
        | "desc" -> Some()
        | _ -> None

    let (|YesNo|_|) s =
        match String.toLower s with
        | "yes" -> Some true
        | "no" -> Some false
        | _ -> None

type PageUrl = {
    Segments: string list
    Query: (string * string) list
} with
    static member WithSegments([<System.ParamArray>] segments) = {
        Segments = List.ofArray segments // ↩
        Query = []
    }

    static member Root = PageUrl.WithSegments()

    member this.WithQueryParam(name: string, value: string) = // ↩
        { this with Query = (name, value) :: this.Query |> List.sort }

    member this.WithQueryParamNotEmpty(name: string, value: string) =
        match value with
        | String.NullOrWhiteSpace -> this
        | _ -> this.WithQueryParam(name, value)

    member this.WithQueryParamOptional(name: string, value: string option) =
        match value with
        | None -> this
        | Some value -> this.WithQueryParamNotEmpty(name, value)

    member this.WithFiltersQueryParams(filters: Filters) =
        this
            .WithQueryParamOptional("search", filters.Search.Term)
            .WithQueryParamOptional("highlight", filters.Search.Highlighting.Key)
            .WithQueryParamOptional("matchCase", filters.Search.CaseMatching.Key)
            .WithQueryParamOptional("sort", filters.SortBy |> Option.map SortBy.key)

    member this.WithSegmentOptional(segment: string option) =
        match segment with
        | Some(String.NotEmpty as segment) -> { this with Segments = this.Segments @ [ segment ] }
        | _ -> this

    member this.QueryString =
        this.Query // ↩
        |> List.map (fun (name, value) -> $"%s{name}=%s{value}")
        |> String.concat "&"

    member this.SegmentsWithQueryString = [
        for segment in this.Segments do
            segment

        match this.QueryString with
        | String.NullOrWhiteSpace -> ()
        | queryString -> $"?%s{queryString}"
    ]

[<RequireQualifiedAccess>]
type Page =
    | About
    | Admin
    | Home
    | Login
    | NotFound of url: string
    | ProductIndex of filters: Filters
    | ProductDetail of SKU

    member this.Key =
        match this with
        | Page.About -> "about"
        | Page.Admin -> "admin"
        | Page.Home -> "home"
        | Page.Login -> "login"
        | Page.NotFound _ -> "not-found"
        | Page.ProductIndex { CategoryFilters = None } -> "products"
        | Page.ProductIndex { CategoryFilters = Some(CategoryFilters.Bazaar _) } -> "bazaar"
        | Page.ProductIndex { CategoryFilters = Some(CategoryFilters.Books _) } -> "books"
        | Page.ProductDetail sku -> $"product-%s{sku.Key}"

    static member CurrentNotFound() = Page.NotFound(url = (Router.currentUrl () |> Router.format))
    static member ProductIndexDefaults = Page.ProductIndex Filters.defaults
    static member ProductIndexDefaultsWith(changeFilters) = Page.ProductIndex(changeFilters Filters.defaults)

/// Feliz.Router function alternative implementations compatible with .NET unit tests
[<AutoOpen>]
module private Safe =

#if FABLE_COMPILER
#else
    [<RequireQualifiedAccess>]
    module Route =
        let (|Query|_|) queryString : (string * string) list option =
            // Regex to split query string into key-value pairs
            let queryPattern = System.Text.RegularExpressions.Regex(@"[?&]([^=&]+)=([^&]*)")
            let matches = queryPattern.Matches(queryString)

            if matches.Count = 0 then
                None
            else
                Some [ for m in matches -> m.Groups[1].Value, m.Groups[2].Value ]
#endif

    [<RequireQualifiedAccess>]
    module Router =
        let currentUrl defaultValue =
            try
                Router.currentUrl () |> Router.format
            with _ ->
                defaultValue

        let formatPath (segments: string list) =
            try
                Router.formatPath segments
            with _ ->
                segments |> String.concat "/"

[<RequireQualifiedAccess>]
module private Route =
    /// Activate pattern to get dashed segments in a string to recognize and extract them.
    let private (|Dashed|) s = // ↩
        s |> String.split '-' |> Array.toList

    /// Recognizes a query parameter by key and extracts its value.
    /// `queryParams` is a list of key-value pairs like the one returned by `Route.Query` Feliz.Router helper.
    let private (|Param|_|) key queryParams = // ↩
        queryParams |> List.tryPick (fun (k, v) -> if k = key then Some v else None)

    let (|Author|) queryParams : OLID option =
        match queryParams with
        | Param "author" (String.NotEmpty as olid) -> Some(OLID olid)
        | _ -> None

    let (|Category|) queryParams : BazaarCategory option =
        match queryParams with
        | Param "category" storeCategoryKey -> BazaarCategory.tryFromKey storeCategoryKey
        | _ -> None

    let private (|Highlight|) queryParams : Highlighting =
        match queryParams with
        | Param "highlight" (YesNo false) -> Highlighting.None
        | _ -> Highlighting.Active

    let private (|MatchCase|) queryParams : CaseMatching =
        match queryParams with
        | Param "matchCase" (YesNo true) -> CaseSensitive
        | _ -> CaseInsensitive

    let private (|SearchTerm|) queryParams : string option =
        match queryParams with
        | Param "search" searchTerm -> Some searchTerm
        | _ -> None

    let private (|Sort|) queryParams : (Column * SortDirection) option =
        match queryParams with
        | Param "sort" (Dashed [ Col col; Desc ]) -> Some(col, Descending)
        | Param "sort" (Col col) -> Some(col, Ascending)
        | _ -> None

    /// This active pattern returns a function to change Filters according to the query parameters.
    let (|Filters|) queryParams : Filters -> Filters =
        match queryParams with
        | Highlight highlighting & MatchCase caseMatching & SearchTerm searchTerm & Sort sortBy ->
            fun filters -> {
                filters with
                    Search.Highlighting = highlighting
                    Search.CaseMatching = caseMatching
                    Search.Term = searchTerm
                    SortBy = sortBy
            }

    let (|SKU|_|) routeSegment : SKU option =
        match routeSegment with
        | Dashed [ "FS"; Route.Int fsid ] -> Some (FSID fsid).AsSKU
        | Dashed [ "BN"; isbn ] -> Some (ISBN isbn).AsSKU
        | _ -> None

    let (|Tag|) queryParams : string option =
        match queryParams with
        | Param "tag" (String.NotEmpty as tag) -> Some tag
        | _ -> None

[<RequireQualifiedAccess>]
module Page =
    let defaultPage = Page.Login

    let parseFromUrlSegments segments =
        match segments with
        | [] -> Page.Home
        | [ "about" ] -> Page.About
        | [ "admin" ] -> Page.Admin
        | [ "login" ] -> Page.Login
        | [ "notfound" ] -> Page.NotFound(url = String.empty)
        | [ "notfound"; Route.Query [ "url", url ] ] -> Page.NotFound url

        // ProductDetail
        | [ "bazaar"; Route.SKU sku ] -> Page.ProductDetail sku
        | [ "books"; Route.SKU sku ] -> Page.ProductDetail sku

        // ProductIndex
        | [ "products" ] -> Page.ProductIndexDefaults
        | [ "bazaar" ] -> Page.ProductIndexDefaultsWith _.ToBazaar()
        | [ "books" ] -> Page.ProductIndexDefaultsWith _.ToBooks()
        | [ "bazaar"; Route.Query(Route.Filters changeFilters & Route.Category category) ] ->
            Page.ProductIndexDefaultsWith(fun filters -> { changeFilters filters with CategoryFilters = Some(CategoryFilters.Bazaar category) })

        | [ "books"; Route.Query(Route.Filters changeFilters & Route.Author authorId & Route.Tag tag) ] ->
            Page.ProductIndexDefaultsWith(fun filters -> { changeFilters filters with CategoryFilters = Some(CategoryFilters.Books(authorId, tag)) })

        | _ -> Page.NotFound(Router.formatPath segments)

let (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Admin -> PageUrl.WithSegments("admin")
    | Page.Home -> PageUrl.Root
    | Page.Login -> PageUrl.WithSegments("login")
    | Page.NotFound url -> PageUrl.WithSegments("notfound").WithQueryParamNotEmpty("url", url)

    | Page.ProductDetail({ Type = SKUType.FSID _ } as sku) -> PageUrl.WithSegments("bazaar", sku.Key)
    | Page.ProductDetail({ Type = SKUType.ISBN _ } as sku) -> PageUrl.WithSegments("books", sku.Key)
    | Page.ProductDetail { Type = SKUType.Unknown } -> PageUrl.WithSegments("notfound").WithQueryParam("url", Router.currentUrl "unknown")

    | Page.ProductIndex { CategoryFilters = None } -> PageUrl.WithSegments("products")
    | Page.ProductIndex(filters = { CategoryFilters = Some(CategoryFilters.Bazaar category) } as filters) ->
        PageUrl // ↩
            .WithSegments("bazaar")
            .WithQueryParamOptional("category", category |> Option.map BazaarCategory.key)
            .WithFiltersQueryParams(filters)
    | Page.ProductIndex({ CategoryFilters = Some(CategoryFilters.Books(authorId, tag)) } as filters) ->
        PageUrl
            .WithSegments("books")
            .WithQueryParamOptional("author", authorId |> Option.map (fun (OLID authorId) -> authorId))
            .WithQueryParamOptional("tag", tag)
            .WithFiltersQueryParams(filters)

[<RequireQualifiedAccess>]
module Router =
    let goToUrl (e: MouseEvent) =
        e.preventDefault ()
        let href: string = !!e.currentTarget?attributes?href?value
        Router.navigatePath href

    let navigatePage (PageUrl pageUrl) = Router.navigatePath (pageUrl.Segments, queryString = pageUrl.Query)

[<RequireQualifiedAccess>]
module Cmd =
    let navigatePage (PageUrl pageUrl) = Cmd.navigatePath (pageUrl.Segments, queryString = pageUrl.Query)