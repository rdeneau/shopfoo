module Shopfoo.Client.Routing

open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop
open Shopfoo.Client.Shared
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
    module ProductSort =
        let key =
            function
            | ProductSort.Num -> "num"
            | ProductSort.Title -> "name"
            | ProductSort.BookAuthors -> "authors"
            | ProductSort.StoreCategory -> "category"

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

    [<RequireQualifiedAccess>]
    module StoreCategory =
        let key =
            function
            | StoreCategory.Clothing -> "clothing"
            | StoreCategory.Electronics -> "electronics"
            | StoreCategory.Jewelry -> "jewelry"

        let tryFromKey (key: string) : StoreCategory option =
            match key.ToLowerInvariant() with
            | "clothing" -> Some StoreCategory.Clothing
            | "electronics" -> Some StoreCategory.Electronics
            | "jewelry" -> Some StoreCategory.Jewelry
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
    | ProductIndex
    | ProductBazaar of category: StoreCategory option * searchTerm: string option * sortBy: (ProductSort * SortDirection) option
    | ProductBooks of authorId: OLID option * searchTerm: string option * sortBy: (ProductSort * SortDirection) option
    | ProductDetail of SKU

    member this.Key =
        match this with
        | Page.About -> "about"
        | Page.Admin -> "admin"
        | Page.Home -> "home"
        | Page.Login -> "login"
        | Page.NotFound _ -> "not-found"
        | Page.ProductIndex -> "products"
        | Page.ProductBazaar _ -> "products-bazaar"
        | Page.ProductBooks _ -> "products-books"
        | Page.ProductDetail sku -> $"product-%s{sku.Key}"

    static member CurrentNotFound() =
        Router.currentUrl () |> Router.format |> Page.NotFound

[<RequireQualifiedAccess>]
module private Route =
    let (|Author|) s : OLID option =
        match s with
        | Route.Query [ "author", (String.NotEmpty as olid) ] -> Some(OLID olid)
        | _ -> None

    let (|Category|) s : StoreCategory option =
        match String.toLower s with
        | Route.Query [ "category", storeCategoryKey ] -> StoreCategory.tryFromKey storeCategoryKey
        | _ -> None

    let private (|Dashed|) s = // ↩
        s |> String.split '-' |> Array.toList

    let (|Search|) s : string option =
        match s with
        | Route.Query [ "search", searchTerm ] -> Some searchTerm
        | _ -> None

    let (|Sort|) s : (ProductSort * SortDirection) option =
        match s with
        | Route.Query [ "sort", Dashed [ Column col; Desc ] ] -> Some(col, Descending)
        | Route.Query [ "sort", Column col ] -> Some(col, Ascending)
        | _ -> None

    let (|SKU|_|) s : SKU option =
        match s with
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
        | [ "bazaar" ] -> Page.ProductBazaar(category = None, searchTerm = None, sortBy = None)
        | [ "bazaar"; Route.SKU sku ] -> Page.ProductDetail sku
        | [ "bazaar"; (Route.Category category & Route.Search searchTerm & Route.Sort sortBy) ] -> Page.ProductBazaar(category, searchTerm, sortBy)
        | [ "books" ] -> Page.ProductBooks(authorId = None, searchTerm = None, sortBy = None)
        | [ "books"; Route.SKU sku ] -> Page.ProductDetail sku
        | [ "books"; (Route.Author authorId & Route.Search searchTerm & Route.Sort sortBy) ] -> Page.ProductBooks(authorId, searchTerm, sortBy)
        | [ "login" ] -> Page.Login
        | [ "notfound"; Route.Query [ "url", url ] ] -> Page.NotFound url
        | [ "products" ] -> Page.ProductIndex
        | segments -> Page.NotFound(Router.formatPath segments)

let (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Admin -> PageUrl.WithSegments("admin")
    | Page.Home -> PageUrl.Root
    | Page.Login -> PageUrl.WithSegments("login")
    | Page.NotFound url -> PageUrl.WithSegments("notfound").WithQueryParam("url", url)

    | Page.ProductBazaar(storeCategory, searchTerm, sortBy) ->
        PageUrl
            .WithSegments("bazaar")
            .WithQueryParamOptional("category", storeCategory |> Option.map StoreCategory.key)
            .WithQueryParamOptional("search", searchTerm)
            .WithQueryParamOptional("sort", sortBy |> Option.map SortBy.key)

    | Page.ProductBooks(authorId, searchTerm, sortBy) ->
        PageUrl
            .WithSegments("books")
            .WithQueryParamOptional("author", authorId |> Option.map (fun (OLID authorId) -> authorId))
            .WithQueryParamOptional("search", searchTerm)
            .WithQueryParamOptional("sort", sortBy |> Option.map SortBy.key)

    | Page.ProductIndex -> PageUrl.WithSegments("products")
    | Page.ProductDetail({ Type = SKUType.FSID _ } as sku) -> PageUrl.WithSegments("bazaar", sku.Key)
    | Page.ProductDetail({ Type = SKUType.ISBN _ } as sku) -> PageUrl.WithSegments("books", sku.Key)
    | Page.ProductDetail { Type = SKUType.Unknown } -> PageUrl.WithSegments("notfound").WithQueryParam("url", Router.currentUrl () |> Router.format)

[<RequireQualifiedAccess>]
module Router =
    let goToUrl (e: MouseEvent) =
        e.preventDefault ()
        let href: string = !!e.currentTarget?attributes?href?value
        Router.navigatePath href

    let navigatePage (PageUrl pageUrl) =
        Router.navigatePath (pageUrl.Segments, queryString = pageUrl.Query)

[<RequireQualifiedAccess>]
module Cmd =
    let navigatePage (PageUrl pageUrl) =
        Cmd.navigatePath (pageUrl.Segments, queryString = pageUrl.Query)