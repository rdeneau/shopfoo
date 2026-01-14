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
    module ProductSort =
        let key =
            function
            | ProductSort.Num -> "num"
            | ProductSort.Title -> "name"
            // TODO RDE: | ProductSort.BookTags -> "tags"
            | ProductSort.BookAuthors -> "authors"
            | ProductSort.StoreCategory -> "category"
            // TODO RDE: | ProductSort.SKU -> "sku"

    let (|Column|_|) key : ProductSort option =
        match String.toLower key with
        | "num" -> Some ProductSort.Num
        | "name" -> Some ProductSort.Title
        | "authors" -> Some ProductSort.BookAuthors
        | "category" -> Some ProductSort.StoreCategory
        | _ -> None

    [<RequireQualifiedAccess>]
    module SortBy =
        let key =
            function
            | col, Ascending -> col |> ProductSort.key
            | col, Descending -> $"{col |> ProductSort.key}-desc"

    let (|Desc|_|) s =
        match String.toLower s with
        | "desc" -> Some()
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
        { this with Query = (name, value) :: this.Query }

    member this.WithQueryParamOptional(name: string, value: string option) =
        match value with
        | None -> this
        | Some value -> this.WithQueryParam(name, value)

    member this.WithSegmentOptional(segment: string option) =
        match segment with
        | Some(String.NotEmpty as segment) -> { this with Segments = this.Segments @ [ segment ] }
        | _ -> this

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
        | Page.ProductIndex _ -> "products"
        | Page.ProductDetail sku -> $"product-%s{sku.Key}"

    static member CurrentNotFound() = // ↩
        Page.NotFound(url = (Router.currentUrl () |> Router.format))

[<RequireQualifiedAccess>]
module private Route =
    let (|Author|) queryString : OLID option =
        match queryString with
        | Route.Query [ "author", (String.NotEmpty as olid) ] -> Some(OLID olid)
        | _ -> None

    let (|Category|) queryString : BazaarCategory option =
        match String.toLower queryString with
        | Route.Query [ "category", storeCategoryKey ] -> BazaarCategory.tryFromKey storeCategoryKey
        | _ -> None

    let private (|Dashed|) s = // ↩
        s |> String.split '-' |> Array.toList

    let (|Search|) queryString : string option =
        match queryString with
        | Route.Query [ "search", searchTerm ] -> Some searchTerm
        | _ -> None

    let (|Sort|) queryString : (ProductSort * SortDirection) option =
        match queryString with
        | Route.Query [ "sort", Dashed [ Column col; Desc ] ] -> Some(col, Descending)
        | Route.Query [ "sort", Column col ] -> Some(col, Ascending)
        | _ -> None

    let (|SKU|_|) routeSegment : SKU option =
        match routeSegment with
        | Dashed [ "FS"; Route.Int fsid ] -> Some (FSID fsid).AsSKU
        | Dashed [ "BN"; isbn ] -> Some (ISBN isbn).AsSKU
        | _ -> None

[<RequireQualifiedAccess>]
module Page =
    let defaultPage = Page.Login

    let parseFromUrlSegments =
        function
        | [] -> Page.Home
        | [ "about" ] -> Page.About
        | [ "admin" ] -> Page.Admin
        | [ "login" ] -> Page.Login
        | [ "notfound"; Route.Query [ "url", url ] ] -> Page.NotFound url

        // ProductDetail
        | [ "bazaar"; Route.SKU sku ] -> Page.ProductDetail sku
        | [ "books"; Route.SKU sku ] -> Page.ProductDetail sku

        // ProductIndex
        | [ "products" ] -> Page.ProductIndex Filters.none
        | [ "bazaar" ] -> Page.ProductIndex(Filters.none.ToBazaar())
        | [ "books" ] -> Page.ProductIndex(Filters.none.ToBooks())
        | [ "bazaar"; (Route.Category category & Route.Search searchTerm & Route.Sort sortBy) ] ->
            Page.ProductIndex {
                Filters.none with
                    CategoryFilters = Some(CategoryFilters.Bazaar category)
                    SearchTerm = searchTerm
                    SortBy = sortBy
            }

        | [ "books"; (Route.Author authorId & Route.Tag tag & Route.Search searchTerm & Route.Sort sortBy) ] ->
            Page.ProductIndex {
                Filters.none with
                    CategoryFilters = Some(CategoryFilters.Books(authorId, tag))
                    SearchTerm = searchTerm
                    SortBy = sortBy
            }

        | segments -> Page.NotFound(Router.formatPath segments)

let (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Admin -> PageUrl.WithSegments("admin")
    | Page.Home -> PageUrl.Root
    | Page.Login -> PageUrl.WithSegments("login")
    | Page.NotFound url -> PageUrl.WithSegments("notfound").WithQueryParam("url", url)

    | Page.ProductDetail({ Type = SKUType.FSID _ } as sku) -> PageUrl.WithSegments("bazaar", sku.Key)
    | Page.ProductDetail({ Type = SKUType.ISBN _ } as sku) -> PageUrl.WithSegments("books", sku.Key)
    | Page.ProductDetail { Type = SKUType.Unknown } -> PageUrl.WithSegments("notfound").WithQueryParam("url", Router.currentUrl () |> Router.format)

    | Page.ProductIndex { CategoryFilters = None } -> PageUrl.WithSegments("products")
    | Page.ProductIndex(filters = { CategoryFilters = Some(CategoryFilters.Bazaar category) } as filters) ->
        PageUrl
            .WithSegments("bazaar")
            .WithQueryParamOptional("category", category |> Option.map BazaarCategory.key)
            .WithQueryParamOptional("search", filters.SearchTerm)
            .WithQueryParamOptional("sort", filters.SortBy |> Option.map SortBy.key)
    | Page.ProductIndex({ CategoryFilters = Some(CategoryFilters.Books(authorId, tag)) } as filters) ->
        PageUrl
            .WithSegments("books")
            .WithQueryParamOptional("author", authorId |> Option.map (fun (OLID authorId) -> authorId))
            .WithQueryParamOptional("tag", tag)
            .WithQueryParamOptional("search", filters.SearchTerm)
            .WithQueryParamOptional("sort", filters.SortBy |> Option.map SortBy.key)

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