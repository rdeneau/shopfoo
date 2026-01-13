module Shopfoo.Client.Shared

[<RequireQualifiedAccess>]
type ProductSort =
    | Num
    | Title
    | BookAuthors
    // TODO RDE: | BookTags
    | StoreCategory

type SortDirection =
    | Ascending
    | Descending

    member this.Toggle() =
        match this with
        | Ascending -> Descending
        | Descending -> Ascending

[<RequireQualifiedAccess>]
type SortKeyPart =
    | Num of int
    | Text of string