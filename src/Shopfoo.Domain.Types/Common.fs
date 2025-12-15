[<AutoOpen>]
module Shopfoo.Domain.Types.Common

[<RequireQualifiedAccess>]
type Lang =
    | English
    | French

[<RequireQualifiedAccess>]
module Lang =
    let code =
        function
        | Lang.English -> "en"
        | Lang.French -> "fr"