module Shopfoo.Domain.Types.Security

open Shopfoo.Common

type Access =
    | View
    | Edit

type Feat =
    | About
    | Admin
    | Catalog
    | Sales
    | Warehouse

type Claims = Map<Feat, Access>

[<RequireQualifiedAccess>]
module Claims =
    let none: Claims = Map.empty
    let single feat access : Claims = Map [ feat, access ]

    let toSet (claims: Claims) =
        Set [
            for KeyValue(feat, access) in claims do
                feat, access

                if access = Access.Edit then
                    feat, Access.View
        ]

[<RequireQualifiedAccess>]
type User =
    | Anonymous
    | LoggedIn of userName: string * claims: Claims

    member user.AccessTo feat =
        match user with
        | User.LoggedIn(_, claims) -> claims |> Map.tryFind feat
        | _ -> None

    member user.CanAccess feat = user.AccessTo feat |> Option.isSome

let (|UserCanAccess|_|) feat (user: User) = // ↩
    user.CanAccess feat |> Option.ofBool

let (|UserCanNotAccess|_|) feat (user: User) =
    not (user.CanAccess feat) |> Option.ofBool

[<RequireQualifiedAccess>]
module UserNames =
    let guest = "Guest"
    let catalogEditor = "Catalog Editor"
    let sales = "Sales"
    let productManager = "Product Manager"
    let administrator = "Administrator"

type AuthToken = AuthToken of string

type AuthError =
    | TokenInvalid
    | UserUnauthorized