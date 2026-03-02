namespace Shopfoo.Product.Tests.Data

open Shopfoo.Product.Data.OpenLibrary
open Swensen.Unquote
open TUnit.Core

type OpenLibraryShould() =
    [<Test>]
    [<Arguments("OL2653686A")>]
    [<Arguments("/OL2653686A")>]
    [<Arguments("authors/OL2653686A")>]
    [<Arguments("/authors/OL2653686A")>]
    member _.``make sanitized AuthorKey`` key =
        let result = AuthorKey.Make key
        result.Path =! "/authors/OL2653686A"