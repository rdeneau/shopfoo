module Shopfoo.Domain.Types.Security

type Access =
    | View
    | Edit
    // TODO: [Claims] 3b. remove Access.Admin
    | Admin

// TODO: [Claims] 3a. add Feat.Admin
type Feat =
    | Home
    | Catalog
    | Sales
    | Warehouse

// TODO: [Claims] 3c. change type: Map<Feat, Access> (Access.Edit implies Access.View)
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