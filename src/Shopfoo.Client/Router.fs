module Shopfoo.Client.Router

open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop

type private PageUrl = {
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
    | Index

    member this.Key =
        match this with
        | Page.Index -> "index"
        | Page.About -> "about"

[<RequireQualifiedAccess>]
module Page =
    let defaultPage = Page.Index

    let parseFromUrlSegments =
        function
        | [ "about" ] -> Page.About
        | [] -> Page.Index
        | _ -> defaultPage

let private (|PageUrl|) =
    function
    | Page.About -> PageUrl.WithSegments("about")
    | Page.Index -> PageUrl.Root

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