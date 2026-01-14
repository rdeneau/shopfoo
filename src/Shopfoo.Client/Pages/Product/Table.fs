module Shopfoo.Client.Pages.Product.Table

open System
open System.Text.RegularExpressions
open Feliz
open Feliz.DaisyUI
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Pages.Product.Filters
open Shopfoo.Client.Routing
open Shopfoo.Client.Shared
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type Col =
    | Num
    | Name
    | Authors
    | Category
    | Description
// TODO RDE: Col.SKU
// TODO RDE: Col.Tags

type ColDef = { // ↩
    Col: Col
    SortableBy: ProductSort option
}

type Col with
    member col.NotSortable: ColDef = { Col = col; SortableBy = None }
    member col.SortableBy(key) : ColDef = { Col = col; SortableBy = Some key }

let private (|ColSorted|_|) (colDef, filters: Filters) =
    match colDef.SortableBy, filters.SortBy with
    | Some key, Some(currentKey, dir) when key = currentKey -> Some dir
    | _ -> None

type private Row with
    member row.Key = $"product-%i{row.Index}"

type private Td(filters: Filters, translations: AppTranslations, row: Row) =
    let book =
        match row.Product.Category with
        | Category.Books book ->
            Some {|
                Subtitle = book.Subtitle // ↩
                Authors = book.Authors |> List.map _.Name |> String.concat ", "
            |}
        | _ -> None

    let highlight (fullText: string) (key: string) = [
        let term = filters.SearchTerm

        match term with
        | None
        | Some String.NullOrWhiteSpace ->
            // No highlighting
            Html.text fullText

        | Some term ->
            // Highlight occurrences of 'term' in 'fullText' (case-insensitive)

            // If fullText = "Clean Code" and term = "code" then
            // - parts = [| "Clean "; "" |]
            // - matches = [| "Code" |]
            // Result: <span>Clean </span><mark class="...">Code</mark>
            let pattern = Regex.Escape(term)
            let options = RegexOptions.IgnoreCase ||| RegexOptions.Multiline
            let parts = Regex.Split(fullText, pattern, options)
            let matches = Regex.Matches(fullText, pattern, options) |> Seq.toArray

            for i in 0 .. parts.Length - 1 do
                // Regular text
                Html.text parts[i]

                // Matched text (highlighted)
                if i < matches.Length then
                    Html.mark [
                        prop.key $"%s{key}-match-%i{i}"
                        prop.className "bg-yellow-200 text-black rounded-sm"
                        prop.text matches[i].Value
                    ]
    ]

    member _.authors =
        Html.td [
            prop.key $"%s{row.Key}-authors"
            prop.children [
                Html.div [
                    prop.key $"%s{row.Key}-authors-content"
                    prop.className "w-40 line-clamp-2 group-hover:line-clamp-3"
                    match book with
                    | Some book -> prop.text book.Authors
                    | None -> prop.text " "
                ]
            ]
        ]

    member _.category =
        Html.td [
            prop.key $"%s{row.Key}-category"
            prop.className "w-30"
            match row.Product.Category with
            | Category.Store storeProduct -> prop.text (translations.Product.StoreCategoryOf storeProduct.Category)
            | _ -> ()
        ]

    member _.description =
        Html.td [
            prop.key $"%s{row.Key}-desc"
            prop.children [
                Html.div [
                    prop.key $"%s{row.Key}-desc-content"
                    prop.className "line-clamp-2 group-hover:line-clamp-3"
                    prop.children (highlight row.Product.Description $"%s{row.Key}-desc-text")
                ]
            ]
        ]

    member _.num =
        Html.td [ // ↩
            prop.key $"%s{row.Key}-num"
            prop.text (row.Index + 1)
        ]

    member _.name =
        Html.td [
            prop.key $"%s{row.Key}-name"
            prop.children [
                Html.div [
                    prop.key $"%s{row.Key}-name-content"
                    prop.className "w-60 line-clamp-2 group-hover:line-clamp-3"

                    prop.children [
                        Html.div [
                            prop.key $"%s{row.Key}-title"
                            prop.className "group-hover:inline"
                            prop.children (highlight row.Product.Title $"%s{row.Key}-title-text")
                        ]

                        match book with
                        | Some book when not (String.IsNullOrWhiteSpace book.Subtitle) ->
                            Html.span [
                                prop.key $"%s{row.Key}-sep"
                                prop.className "hidden group-hover:inline"
                                prop.text ":"
                            ]

                            Html.div [
                                prop.key $"%s{row.Key}-subtitle"
                                prop.className "italic ml-1 group-hover:inline"
                                prop.children (highlight book.Subtitle $"%s{row.Key}-subtitle-text")
                            ]
                        | _ -> ()
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let IndexTable (filters: Filters) products provider (translations: AppTranslations) =
    let columnDefinitions = [
        Col.Num.SortableBy ProductSort.Num
        // TODO RDE: Col.SKU.SortableBy ProductSort.SKU

        if provider = FakeStore then
            Col.Category.SortableBy ProductSort.StoreCategory

        Col.Name.SortableBy ProductSort.Title

        if provider = OpenLibrary then
            // TODO RDE: Col.Tags.SortableBy ProductSort.Tags
            Col.Authors.SortableBy ProductSort.BookAuthors

        Col.Description.NotSortable
    ]

    let th (colDef: ColDef) =
        let key, text =
            match colDef.Col with
            | Col.Num -> "num", "#"
            | Col.Name -> "name", translations.Product.Name
            | Col.Authors -> "authors", translations.Product.Authors
            | Col.Category -> "category", translations.Product.Category
            | Col.Description -> "description", translations.Product.Description

        Html.th [
            prop.key $"product-th-%s{key}"
            prop.scope "col"

            if colDef.SortableBy.IsSome then
                prop.className "cursor-pointer hover:bg-base-200 transition-colors select-none"

                prop.onClick (fun _ ->
                    let nextDir =
                        match colDef, filters with
                        | ColSorted dir -> dir.Toggle()
                        | _ -> Ascending

                    let nextSortBy = colDef.SortableBy |> Option.map (fun key -> key, nextDir)

                    let nextPage =
                        match provider with
                        | OpenLibrary -> Page.ProductBooks(filters.BooksAuthorId, filters.SearchTerm, nextSortBy)
                        | FakeStore -> Page.ProductBazaar(filters.StoreCategory, filters.SearchTerm, nextSortBy)

                    Router.navigatePage nextPage
                )

            prop.children [
                Html.div [
                    prop.key $"product-th-%s{key}-content"
                    if colDef.SortableBy.IsNone then
                        prop.text $"%s{text}"
                    else
                        let activeSortIcon =
                            function
                            | Ascending -> fa6Solid.sortUp
                            | Descending -> fa6Solid.sortDown

                        let textColor, sortIcon =
                            match colDef, filters with
                            | ColSorted dir -> "text-accent", icon (activeSortIcon dir)
                            | _ -> "text-base-content/30", icon fa6Solid.sort

                        prop.className "flex items-center gap-2"

                        prop.children [
                            Html.text $"%s{text}"
                            Html.span [
                                prop.key $"product-th-%s{key}-sort-icon"
                                prop.className textColor
                                prop.children sortIcon
                            ]
                        ]
                ]
            ]
        ]

    Daisy.table [
        prop.key "product-table"
        prop.className "table-pin-rows w-full border-collapse"
        prop.children [
            Html.thead [
                prop.key "product-thead"
                prop.child (
                    Html.tr [
                        for colDef in columnDefinitions do
                            th colDef
                    ]
                )
            ]
            Html.tbody [
                prop.key "product-tbody"
                prop.children [
                    for row in filters |> Filters.apply products provider translations do
                        Html.tr [
                            prop.key row.Key
                            prop.className "group hover:bg-accent hover:fg-accent hover:cursor-pointer"
                            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail row.Product.SKU))
                            prop.children [
                                let td = Td(filters, translations, row)

                                for colDef in columnDefinitions do
                                    match colDef.Col with
                                    | Col.Num -> td.num
                                    | Col.Name -> td.name
                                    | Col.Authors -> td.authors
                                    | Col.Category -> td.category
                                    | Col.Description -> td.description
                            ]
                        ]
                ]
            ]
        ]
    ]