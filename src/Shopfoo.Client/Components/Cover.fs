namespace Shopfoo.Client.Components

open Fable.Core
open Feliz
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type CoverContainer =
    | None
    | LinkToAuthorCard

[<RequireQualifiedAccess>]
type CoverSize =
    | Small
    | Medium
    | Large

module private CoverSize =
    let code =
        function
        | CoverSize.Small -> "S"
        | CoverSize.Medium -> "M"
        | CoverSize.Large -> "L"

    let defaultWidth =
        function
        | CoverSize.Small -> Some 40
        | CoverSize.Medium -> Some 140
        | CoverSize.Large -> None

    let defaultHeight =
        function
        | CoverSize.Small -> Some 60
        | CoverSize.Medium -> Some 200
        | CoverSize.Large -> None

[<Erase>]
type Cover =
    [<ReactComponent>]
    static member BookAuthor(key, author: BookAuthor, translations: AppTranslations, ?coverSize, ?container, ?sizeCssClass: string) =
        let coverSize = defaultArg coverSize CoverSize.Small
        let container = defaultArg container CoverContainer.None

        let coverUrl = $"https://covers.openlibrary.org/a/olid/%s{author.OLID.Value}-%s{CoverSize.code coverSize}.jpg"
        let defaultUrl = "https://openlibrary.org/static/images/icons/avatar_author-lg.png"
        let authorCardUrl = $"https://openlibrary.org/authors/%s{author.OLID.Value}"

        let img =
            Html.img [
                prop.key $"%s{key}-img"
                prop.className [
                    "object-cover object-top rounded-sm"
                    match sizeCssClass with
                    | Some s -> s
                    | None -> ()
                ]
                prop.src $"%s{coverUrl}?default=%s{defaultUrl}"
                prop.alt $"%s{author.Name} cover"
            ]

        match container with
        | CoverContainer.None -> // ↩
            img
        | CoverContainer.LinkToAuthorCard ->
            Html.a [
                prop.key $"%s{key}-link"
                prop.href authorCardUrl
                prop.target "_blank"
                prop.rel "noopener noreferrer"
                prop.className "ml-2 my-0.5 shrink-0 hover:opacity-80 transition-opacity"
                prop.title (translations.Product.OpenLibraryAuthorPage author.Name)
                prop.children [ img ]
            ]