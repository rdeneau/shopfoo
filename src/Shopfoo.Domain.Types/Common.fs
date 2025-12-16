[<AutoOpen>]
module Shopfoo.Domain.Types.Common

[<RequireQualifiedAccess>]
type Lang =
    | English
    | French
    | Latin

[<RequireQualifiedAccess>]
module Lang =
    let code =
        function
        | Lang.English -> "en"
        | Lang.French -> "fr"
        | Lang.Latin -> "la"