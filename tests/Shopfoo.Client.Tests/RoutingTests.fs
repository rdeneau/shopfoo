namespace Shopfoo.Client.Tests

open Shopfoo.Client.Routing
open Shopfoo.Client.Tests.FsCheckArbs
open Swensen.Unquote
open TUnit.Core

type RoutingTests() =
    [<Test; ShopfooFsCheckProperty>]
    member _.``roundtrip page``(sanitizedPage: SanitizedPage) =
        let inputPage = sanitizedPage.Value
        let (PageUrl pageUrl) = inputPage

        let parsedPage = Page.parseFromUrlSegments pageUrl.SegmentsWithQueryString
        parsedPage =! inputPage