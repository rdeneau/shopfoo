module Shopfoo.Client.Pages.Product.Index.ProductsTable

open Feliz
open Feliz.DaisyUI
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Routing
open Shopfoo.Client.Filters
open Shopfoo.Client.Search
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Shared.Translations

type ColDef = { // ↩
    Column: Column
    SortableBy: Column option
}

type Column with
    member col.NotSortable: ColDef = { Column = col; SortableBy = None }
    member col.SortableBy(key) : ColDef = { Column = col; SortableBy = Some key }

let private (|ColSorted|_|) (colDef, filters: Filters) =
    match colDef.SortableBy, filters.SortBy with
    | Some key, Some(currentKey, dir) when key = currentKey -> Some dir
    | _ -> None

type private Row with
    member row.Key = $"product-%i{row.Index}"

type private Td(filters: Filters, row: Row) =
    let bazaarProduct =
        match row.Product.Category with
        | Category.Bazaar bazaarProduct -> Some bazaarProduct
        | Category.Books _ -> None

    member _.num =
        Html.td [ // ↩
            prop.key $"%s{row.Key}-num"
            prop.text (row.Index + 1)
        ]

    member _.sku =
        Html.td [
            prop.key $"%s{row.Key}-sku"
            prop.children [
                for i, result in List.indexed row.SearchResult[Column.SKU] do
                    result |> Highlight.matches Html.span [ prop.key $"%s{row.Key}-sku-%i{i}" ]
            ]
        ]

    member _.bazaarCategory =
        Html.td [
            prop.key $"%s{row.Key}-category"
            prop.className "w-30"
            prop.children [
                for i, result in List.indexed row.SearchResult[Column.BazaarCategory] do
                    result
                    |> Highlight.matches Html.span [
                        prop.key $"%s{row.Key}-category-%i{i}"
                        if (bazaarProduct |> Option.map _.Category) = filters.BazaarCategory then
                            prop.className $"%s{Highlight.Css.TextColors} %s{Highlight.Css.Border}"
                    ]
            ]
        ]

    member _.bookAuthors =
        Html.td [
            prop.key $"%s{row.Key}-authors"
            prop.children [
                Html.div [
                    prop.key $"%s{row.Key}-authors-content"
                    prop.className "w-40 flex-wrap comma-between-children line-clamp-2 group-hover:line-clamp-3"
                    prop.children [
                        for i, result in List.indexed row.SearchResult[Column.BookAuthors] do
                            match result.Target with
                            | SearchTarget.BookAuthor author ->
                                if i > 0 then
                                    Html.text ", "

                                result
                                |> Highlight.matches Html.span [
                                    prop.key $"%s{row.Key}-author-%i{i}"
                                    if filters.BooksAuthorId = Some author.OLID then
                                        prop.className $"%s{Highlight.Css.TextColors} %s{Highlight.Css.Border}"
                                ]
                            | _ -> ()
                    ]
                ]
            ]
        ]

    member _.bookTags =
        Html.td [
            prop.key $"%s{row.Key}-tags"
            prop.children [
                Html.div [
                    prop.key $"%s{row.Key}-tags-content"
                    prop.className "flex flex-wrap gap-2"
                    prop.children [
                        for i, result in List.indexed row.SearchResult[Column.BookTags] do
                            match result.Target with
                            | SearchTarget.BookTag tag ->
                                Daisy.badge [
                                    badge.ghost
                                    prop.key $"%s{row.Key}-tag-%i{i}"
                                    prop.classes [
                                        "leading-tight px-1 max-w-[150px] group-hover:max-w-none"
                                        if filters.BooksTag = Some tag then
                                            Highlight.Css.BorderColor
                                            Highlight.Css.TextColors
                                    ]
                                    prop.children [
                                        result
                                        |> Highlight.matches Html.span [ // ↩
                                            prop.key $"%s{row.Key}-tags-text"
                                            prop.className "truncate"
                                        ]
                                    ]
                                ]
                            | _ -> ()
                    ]
                ]
            ]
        ]

    member _.description =
        Html.td [
            prop.key $"%s{row.Key}-description"
            prop.children [
                for i, result in List.indexed row.SearchResult[Column.Description] do
                    result
                    |> Highlight.matches Html.span [
                        prop.key $"%s{row.Key}-description-%i{i}"
                        prop.className "line-clamp-2 group-hover:line-clamp-3"
                    ]
            ]
        ]

    member _.name =
        Html.td [
            prop.key $"%s{row.Key}-name"
            prop.children [
                Html.div [
                    prop.key $"%s{row.Key}-name-content"
                    prop.className "w-60 line-clamp-2 group-hover:line-clamp-3"

                    prop.children [
                        for i, result in List.indexed row.SearchResult[Column.Name] do
                            result
                            |> Highlight.matches Html.div [ // ↩
                                prop.key $"%s{row.Key}-title-%i{i}"
                                prop.className "group-hover:inline"
                            ]

                        match row.SearchResult[Column.BookSubtitle] with
                        | [] -> ()
                        | results ->
                            Html.span [
                                prop.key $"%s{row.Key}-sep"
                                prop.className "hidden group-hover:inline"
                                prop.text ":"
                            ]

                            for i, result in List.indexed results do
                                result
                                |> Highlight.matches Html.span [ // ↩
                                    prop.key $"%s{row.Key}-subtitle-%i{i}"
                                    prop.className "italic ml-1 group-hover:inline"
                                ]
                    ]
                ]
            ]
        ]

[<ReactComponent>]
let ProductsTable key (filters: Filters) products provider (translations: AppTranslations) =
    let columnDefinitions = [
        Column.Num.SortableBy Column.Num
        Column.SKU.SortableBy Column.SKU

        if provider = FakeStore then
            Column.BazaarCategory.SortableBy Column.BazaarCategory

        Column.Name.SortableBy Column.Name

        if provider = OpenLibrary then
            Column.BookAuthors.SortableBy Column.BookAuthors
            Column.BookTags.SortableBy Column.BookTags

        Column.Description.NotSortable
    ]

    let th (colDef: ColDef) =
        let key, text =
            match colDef.Column with
            | Column.Num -> "num", "#"
            | Column.SKU -> "sku", "SKU"
            | Column.Name -> "name", translations.Product.Name
            | Column.Description -> "description", translations.Product.Description
            | Column.BazaarCategory -> "category", translations.Product.Category
            | Column.BookSubtitle -> "name", translations.Product.Name // Subtitle is part of Name column
            | Column.BookAuthors -> "authors", translations.Product.Authors
            | Column.BookTags -> "tags", translations.Product.Tags

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
                    let nextPage = Page.ProductIndex { filters with SortBy = nextSortBy }
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
        prop.key $"%s{key}-content"
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
                    for row in filters |> Filters.apply products translations do
                        Html.tr [
                            prop.key row.Key
                            prop.className "group hover:bg-accent hover:fg-accent hover:cursor-pointer"
                            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail row.Product.SKU))
                            prop.children [
                                let td = Td(filters, row)

                                for colDef in columnDefinitions do
                                    match colDef.Column with
                                    | Column.Num -> td.num
                                    | Column.Name -> td.name
                                    | Column.Description -> td.description
                                    | Column.BazaarCategory -> td.bazaarCategory
                                    | Column.BookSubtitle -> td.name // Subtitle is part of Name column
                                    | Column.BookAuthors -> td.bookAuthors
                                    | Column.BookTags -> td.bookTags
                                    | Column.SKU -> td.sku
                            ]
                        ]
                ]
            ]
        ]
    ]