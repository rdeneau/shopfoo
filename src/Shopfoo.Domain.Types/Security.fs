module Shopfoo.Domain.Types.Security

type Access =
    | View
    | Edit
    | Admin

type Feat =
    | Home
    | Catalog
    | Sales
    | Warehouse

type Claims = (Feat * Access) list

[<RequireQualifiedAccess>]
module Claims =
    let none = []

type User =
    | Anonymous
    | Authorized of userName: string * claims: Claims

type AuthToken = AuthToken of string

type AuthError =
    | TokenInvalid
    | UserUnauthorized