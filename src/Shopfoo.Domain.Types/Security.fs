module Shopfoo.Domain.Types.Security

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

let (|UserCanAccess|_|) feat user =
    match user with
    | User.LoggedIn(_, claims) when claims.ContainsKey(feat) -> Some()
    | _ -> None
    | _ -> Some()

type AuthToken = AuthToken of string

type AuthError =
    | TokenInvalid
    | UserUnauthorized