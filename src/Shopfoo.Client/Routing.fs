module Shopfoo.Client.Routing

open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop

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

[<RequireQualifiedAccess>]
type Page =
    | About
    | Admin
    | Home
    | Login
    | NotFound of url: string
    | ProductIndex
    | ProductDetail of sku: string

    member this.Key =
        match this with
        | Page.About -> "about"
        | Page.Admin -> "admin"
        | Page.Home -> "home"
        | Page.Login -> "login"
        | Page.NotFound _ -> "not-found"
        | Page.ProductIndex -> "product"
        | Page.ProductDetail sku -> $"product-{sku}"

    static member CurrentNotFound() =
        Router.currentUrl () |> Router.format |> Page.NotFound

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
        | [ "product" ] -> Page.ProductIndex
        | [ "product"; sku ] -> Page.ProductDetail sku
        | segments -> Page.NotFound(Router.formatPath (segments))

let (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Admin -> PageUrl.WithSegments("admin")
    | Page.Home -> PageUrl.Root
    | Page.Login -> PageUrl.WithSegments("login")
    | Page.NotFound url -> PageUrl.WithSegments("notfound").WithQueryParam("url", url)
    | Page.ProductIndex -> PageUrl.WithSegments("product")
    | Page.ProductDetail sku -> PageUrl.WithSegments("product", sku)

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