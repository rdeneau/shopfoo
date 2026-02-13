namespace Shopfoo.Product.Tests

open Shopfoo.Domain.Types
open Shopfoo.Product.Data.OpenLibrary.Dto
open Swensen.Unquote
open TUnit.Core

type KeySanitizingShould() =
    [<Test>]
    [<Arguments("OL2653686A")>]
    [<Arguments("/OL2653686A")>]
    [<Arguments("authors/OL2653686A")>]
    [<Arguments("/authors/OL2653686A")>]
    member _.``work with AuthorKey`` key =
        let result = AuthorKey.sanitize key
        result.Value =! "/authors/OL2653686A"

    [<Test>]
    [<Arguments("OL2653686A")>]
    [<Arguments("OL31838215M")>]
    member _.``work from author OLID`` olid =
        let olid = OLID olid
        let result = AuthorKey.ofOlid olid
        result.Value =! $"/authors/%s{olid.Value}"