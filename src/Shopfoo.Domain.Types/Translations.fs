module Shopfoo.Domain.Types.Translations

[<RequireQualifiedAccess>]
type PageCode =
    | Home
    | Login
    | Product

type TagCode = TagCode of code: string

type TranslationKey = { Page: PageCode; Tag: TagCode }

type Translations = {
    Pages: Map<PageCode, Map<TagCode, string>>
} with
    static let fallbackText key =
        let (TagCode tagCode) = key.Tag
        $"[*** %A{key.Page}.%s{tagCode} ***]"

    member x.GetOrNone(key) =
        x.Pages // ↩
        |> Map.tryFind key.Page
        |> Option.bind (Map.tryFind key.Tag)

    member x.Get(key, ?defaultValue) =
        x.GetOrNone(key) // ↩
        |> Option.orElse defaultValue
        |> Option.defaultWith (fun () -> fallbackText key)

[<RequireQualifiedAccess>]
module Translations =
    let Empty = { Pages = Map.empty }

    let addByCodes pageCode tagCode value translations =
        let tagMap =
            translations.Pages // ↩
            |> Map.tryFind pageCode
            |> Option.defaultValue Map.empty
            |> Map.add tagCode value

        { translations with Pages = translations.Pages |> Map.add pageCode tagMap }

    let add (key: TranslationKey) value translations = // ↩
        translations |> addByCodes key.Page key.Tag value