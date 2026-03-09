namespace Shopfoo.Client.Tests

open System
open Shopfoo.Client.Filters
open Shopfoo.Client.Routing
open Shopfoo.Client.View
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Security
open Swensen.Unquote
open TUnit.Core

[<AutoOpen>]
module Helpers =
    type FeatEnum =
        | Admin = 1
        | Catalog = 2

    let (|FeatFromEnum|) =
        function
        | FeatEnum.Admin -> Feat.Admin
        | FeatEnum.Catalog -> Feat.Catalog
        | featEnum -> ArgumentOutOfRangeException(nameof featEnum, featEnum, "Unsupported FeatEnum") |> raise

    type PageEnum =
        | About = 1
        | Admin = 2
        | Home = 3
        | Login = 4
        | NotFound = 5
        | ProductIndex = 6
        | ProductDetail = 7

    let (|PageFromEnum|) =
        function
        | PageEnum.About -> Page.About
        | PageEnum.Admin -> Page.Admin
        | PageEnum.Home -> Page.Home
        | PageEnum.Login -> Page.Login
        | PageEnum.NotFound -> Page.NotFound "/unknown"
        | PageEnum.ProductIndex -> Page.ProductIndex Filters.defaults
        | PageEnum.ProductDetail -> Page.ProductDetail (ISBN "1234567890").AsSKU
        | pageEnum -> ArgumentOutOfRangeException(nameof pageEnum, pageEnum, "Unsupported PageEnum") |> raise

    type UserEnum =
        | Anonymous = 1
        | LoggedIn = 2

    let userLoggedIn = User.LoggedIn("alice", Map.empty)

    let (|UserFromEnum|) =
        function
        | UserEnum.Anonymous -> User.Anonymous
        | UserEnum.LoggedIn -> userLoggedIn
        | userEnum -> ArgumentOutOfRangeException(nameof userEnum, userEnum, "Unsupported UserEnum") |> raise

type AppViewShould() =
    [<Test>]
    [<Arguments(PageEnum.About, UserEnum.Anonymous)>]
    [<Arguments(PageEnum.About, UserEnum.LoggedIn)>]
    [<Arguments(PageEnum.Login, UserEnum.Anonymous)>]
    [<Arguments(PageEnum.NotFound, UserEnum.Anonymous)>]
    [<Arguments(PageEnum.NotFound, UserEnum.LoggedIn)>]
    member _.``display the requested page given it's consistent with the authentication`` (PageFromEnum page) (UserFromEnum user) =
        resolvePageAccess page user =! (page, None)

    [<Test>]
    [<Arguments(PageEnum.ProductIndex, FeatEnum.Catalog)>]
    [<Arguments(PageEnum.ProductDetail, FeatEnum.Catalog)>]
    [<Arguments(PageEnum.Admin, FeatEnum.Admin)>]
    member _.``display the requested page with an access check`` (PageFromEnum page) (FeatFromEnum featToCheck) =
        resolvePageAccess page userLoggedIn =! (page, Some featToCheck)

    [<Test>]
    [<Arguments(PageEnum.Home)>]
    [<Arguments(PageEnum.Login)>]
    member _.``redirect to the default page with Catalog access check when logged in``(PageFromEnum page) =
        resolvePageAccess page userLoggedIn =! (Page.ProductIndexDefaults, Some Feat.Catalog)

    [<Test>]
    [<Arguments(PageEnum.Admin)>]
    [<Arguments(PageEnum.Home)>]
    [<Arguments(PageEnum.ProductIndex)>]
    [<Arguments(PageEnum.ProductDetail)>]
    member _.``display the Login page given an anonymous user accesses a page needing a logged-in user``(PageFromEnum page) =
        resolvePageAccess page User.Anonymous =! (Page.Login, None)