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

let (|UserCanAccess|_|) feat (user: User) = user.CanAccess feat |> Option.ofBool
let (|UserCanNotAccess|_|) feat (user: User) = not (user.CanAccess feat) |> Option.ofBool

type AuthToken = AuthToken of string

type AuthError =
    | TokenInvalid
    | UserUnauthorized

[<RequireQualifiedAccess>]
type Persona =
    | Guest
    | CatalogEditor
    | Sales
    | ProductManager
    | Administrator

    member this.Name: string =
        match this with
        | Guest -> "Guest"
        | CatalogEditor -> "Catalog Editor"
        | Sales -> "Sales"
        | ProductManager -> "Product Manager"
        | Administrator -> "Administrator"

    member this.Claims: Claims =
        match this with
        | Guest ->
            Map [ // ↩
                Feat.About, Access.View
                Feat.Catalog, Access.View
            ]
        | CatalogEditor ->
            Map [
                Feat.About, Access.View
                Feat.Catalog, Access.Edit
                Feat.Sales, Access.View
                Feat.Warehouse, Access.View
            ]
        | Sales ->
            Map [
                Feat.About, Access.View
                Feat.Catalog, Access.View
                Feat.Sales, Access.Edit
                Feat.Warehouse, Access.Edit
            ]
        | ProductManager ->
            Map [
                Feat.About, Access.View
                Feat.Catalog, Access.Edit
                Feat.Sales, Access.Edit
                Feat.Warehouse, Access.Edit
            ]
        | Administrator ->
            Map [
                Feat.About, Access.View
                Feat.Catalog, Access.Edit
                Feat.Sales, Access.Edit
                Feat.Warehouse, Access.Edit
                Feat.Admin, Access.Edit
            ]

    static member All = [
        Guest
        CatalogEditor
        Sales
        ProductManager
        Administrator
    ]