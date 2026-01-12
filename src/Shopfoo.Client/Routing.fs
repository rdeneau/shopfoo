module Shopfoo.Client.Routing

open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog

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
    | ProductIndex of categoryKey: string option
    | ProductDetail of skuKey: string

    member this.Key =
        match this with
        | Page.About -> "about"
        | Page.Admin -> "admin"
        | Page.Home -> "home"
        | Page.Login -> "login"
        | Page.NotFound _ -> "not-found"
        | Page.ProductIndex None -> "products"
        | Page.ProductIndex(Some categoryKey) -> $"products-%s{categoryKey}"
        | Page.ProductDetail skuKey -> $"product-%s{skuKey}"

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
        | [ "products" ] -> Page.ProductIndex None
        | [ "products"; categoryKey ] -> Page.ProductIndex(Some categoryKey)
        | [ "product"; skuKey ] -> Page.ProductDetail skuKey
        | segments -> Page.NotFound(Router.formatPath segments)

let (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Admin -> PageUrl.WithSegments("admin")
    | Page.Home -> PageUrl.Root
    | Page.Login -> PageUrl.WithSegments("login")
    | Page.NotFound url -> PageUrl.WithSegments("notfound").WithQueryParam("url", url)
    | Page.ProductIndex None -> PageUrl.WithSegments("products")
    | Page.ProductIndex(Some categoryKey) -> PageUrl.WithSegments("products", categoryKey)
    | Page.ProductDetail skuKey -> PageUrl.WithSegments("product", skuKey)

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

[<AutoOpen>]
module Keys =
    type SKU with
        static member FromKey(skuKey: string) : SKU =
            match skuKey |> String.split '-' with
            | [| "FS"; String.Int fsid |] -> (FSID fsid).AsSKU
            | [| "BN"; isbn |] -> (ISBN isbn).AsSKU
            | _ -> SKUUnknown.SKUUnknown.AsSKU

        member this.Key =
            match this.Type, this.Value with
            | SKUType.FSID _, (String.StartsWith "FS-" as value) -> value
            | SKUType.FSID _, value -> $"FS-%s{value}"
            | SKUType.ISBN _, value -> $"BN-%s{value}"
            | SKUType.Unknown, _ -> ""

    type Provider with
        static member FromCategoryKey(categoryKey: string) : Provider option =
            match categoryKey.ToLowerInvariant() with
            | "bazaar" -> Some Provider.FakeStore
            | "books" -> Some Provider.OpenLibrary
            | _ -> None

        member this.CategoryKey =
            match this with
            | Provider.FakeStore -> "bazaar"
            | Provider.OpenLibrary -> "books"