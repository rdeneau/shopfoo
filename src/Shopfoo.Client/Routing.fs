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

type Page =
    | About
    | Home
    | Login
    | ProductIndex
    | ProductDetail of sku: string

    member this.Key =
        match this with
        | Page.About -> "about"
        | Page.Home -> "home"
        | Page.Login -> "login"
        | Page.ProductIndex -> "product"
        | Page.ProductDetail sku -> $"product-{sku}"

[<RequireQualifiedAccess>]
module Page =
    let defaultPage = Page.Login

    let parseFromUrlSegments =
        function
        | [] -> Page.Home
        | [ "about" ] -> Page.About
        | [ "login" ] -> Page.Login
        | [ "product" ] -> Page.ProductIndex
        | [ "product"; sku ] -> Page.ProductDetail sku
        | segments -> failwith $"Url not supported: %A{segments}"

let (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Home -> PageUrl.Root
    | Page.Login -> PageUrl.WithSegments("login")
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