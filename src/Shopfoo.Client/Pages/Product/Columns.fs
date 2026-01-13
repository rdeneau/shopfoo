module Shopfoo.Client.Pages.Product.Columns

open System
open Feliz
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Pages.Product.Filters
open Shopfoo.Client.Routing
open Shopfoo.Client.Shared
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

let (|ColSorted|_|) (colDef, filters: FiltersModel) =
    match colDef.SortableBy, filters.SortBy with
    | Some key, Some(currentKey, dir) when key = currentKey -> Some dir
    | _ -> None

let columnDefinitionsFor provider = [
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

let private activeSortIcon =
    function
    | Ascending -> fa6Solid.sortUp
    | Descending -> fa6Solid.sortDown

type private Td(filters: Filters, provider, translations: AppTranslations, i, product) =
    let productKey = $"product-%i{i}"

    let book =
        match provider, product.Category with
        | OpenLibrary, Category.Books book ->
            Some {|
                Subtitle = book.Subtitle // ↩
                Authors = book.Authors |> List.map _.Name |> String.concat ", "
            |}
        | _ -> None

    let storeProduct =
        match provider, product.Category with
        | FakeStore, Category.Store storeProduct -> Some storeProduct
        | _ -> None

    member _.authors =
        Html.td [
            prop.key $"%s{productKey}-authors"
            prop.children [
                Html.div [
                    prop.key $"%s{productKey}-authors-content"
                    prop.className "w-40 line-clamp-2 group-hover:line-clamp-3"
                    match book with
                    | Some book -> prop.text book.Authors
                    | None -> prop.text " "
                ]
            ]
        ]

    member _.category =
        Html.td [
            prop.key $"%s{productKey}-category"
            prop.className "w-30"
            match storeProduct with
            | Some { Category = StoreCategory.Clothing } -> prop.text translations.Product.StoreCategory.Clothing
            | Some { Category = StoreCategory.Electronics } -> prop.text translations.Product.StoreCategory.Electronics
            | Some { Category = StoreCategory.Jewelry } -> prop.text translations.Product.StoreCategory.Jewelry
            | None -> ()
        ]

    member _.description =
        Html.td [
            prop.key $"%s{productKey}-desc"
            prop.children [
                Html.div [
                    prop.key $"%s{productKey}-desc-content"
                    prop.className "line-clamp-2 group-hover:line-clamp-3"
                    prop.children (filters.highlight product.Description $"%s{productKey}-desc-text")
                ]
            ]
        ]

    member _.num =
        Html.td [ // ↩
            prop.key $"%s{productKey}-num"
            prop.text (i + 1)
        ]

    member _.name =
        Html.td [
            prop.key $"%s{productKey}-name"
            prop.children [
                Html.div [
                    prop.key $"%s{productKey}-name-content"
                    prop.className "w-60 line-clamp-2 group-hover:line-clamp-3"

                    prop.children [
                        Html.div [
                            prop.key $"%s{productKey}-title"
                            prop.className "group-hover:inline"
                            prop.children (filters.highlight product.Title $"%s{productKey}-title-text")
                        ]

                        match book with
                        | Some book when not (String.IsNullOrWhiteSpace book.Subtitle) ->
                            Html.span [
                                prop.key $"%s{productKey}-sep"
                                prop.className "hidden group-hover:inline"
                                prop.text ":"
                            ]

                            Html.div [
                                prop.key $"%s{productKey}-subtitle"
                                prop.className "italic ml-1 group-hover:inline"
                                prop.children (filters.highlight book.Subtitle $"%s{productKey}-subtitle-text")
                            ]
                        | _ -> ()
                    ]
                ]
            ]
        ]

type Columns(filters: Filters, provider, translations: AppTranslations) =
    member _.th(colDef: ColDef) =
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
                        match colDef, filters.Model with
                        | ColSorted dir -> dir.Toggle()
                        | _ -> Ascending

                    let nextSortBy = colDef.SortableBy |> Option.map (fun key -> key, nextDir)

                    let nextPage =
                        match provider with
                        | OpenLibrary -> Page.ProductBooks(filters.Model.BooksAuthorId, filters.Model.SearchTerm, nextSortBy)
                        | FakeStore -> Page.ProductBazaar(filters.Model.StoreCategory, filters.Model.SearchTerm, nextSortBy)

                    Router.navigatePage nextPage
                )

            prop.children [
                Html.div [
                    prop.key $"product-th-%s{key}-content"
                    if colDef.SortableBy.IsNone then
                        prop.text $"%s{text}"
                    else
                        let textColor, sortIcon =
                            match colDef, filters.Model with
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

    member _.tr i product =
        let productKey = $"product-%i{i}"
        let td = Td(filters, provider, translations, i, product)

        Html.tr [
            prop.key productKey
            prop.className "group hover:bg-accent hover:fg-accent hover:cursor-pointer"
            prop.onClick (fun _ -> Router.navigatePage (Page.ProductDetail product.SKU))
            prop.children [
                for colDef in columnDefinitionsFor provider do
                    match colDef.Col with
                    | Col.Num -> td.num
                    | Col.Name -> td.name
                    | Col.Authors -> td.authors
                    | Col.Category -> td.category
                    | Col.Description -> td.description
            ]
        ]