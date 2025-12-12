module Shopfoo.Domain.Types.Translations

[<RequireQualifiedAccess>]
type PageCode =
    | About
    | Product
    | Shared

type TagCode = TagCode of code: string

type TranslationKey = { Page: PageCode; Tag: TagCode }

type Translations = {
    Pages: Map<PageCode, Map<TagCode, string>>
} with
    static member FallbackText key =
        let (TagCode tagCode) = key.Tag
        $"[*** %A{key.Page}.%s{tagCode} ***]"

    member x.GetOrNone(key) =
        x.Pages // ↩
        |> Map.tryFind key.Page
        |> Option.bind (Map.tryFind key.Tag)

    member x.Get(key, ?defaultValue) =
        x.GetOrNone(key) // ↩
        |> Option.orElse defaultValue
        |> Option.defaultWith (fun () -> Translations.FallbackText key)

    static member Empty = { Pages = Map.empty }

    static member Add (key: TranslationKey, value) x =
        Translations.AddByCodes (key.Page, key.Tag, value) x

    static member AddByCodes (pageCode, tagCode, value) x =
        let tagMap =
            x.Pages |> Map.tryFind pageCode |> Option.defaultValue Map.empty |> Map.add tagCode value

        { x with Pages = x.Pages |> Map.add pageCode tagMap }