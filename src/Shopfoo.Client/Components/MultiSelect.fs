namespace Shopfoo.Client.Components

open Fable.Core
open Feliz
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Search
open Shopfoo.Common
open Shopfoo.Shared.Translations
open type Shopfoo.Client.Components.Checkbox

type private ItemModel<'a> = {
    Key: string
    Text: string
    Value: 'a
    SearchResult: SearchTargetResult
}

[<Erase>]
type MultiSelect =
    [<ReactComponent>]
    static member MultiSelect
        (
            key: string, // ↩
            items: Set<'a>,
            selectedItems: Set<'a>,
            formatItem: 'a -> string,
            onSelect: bool * 'a -> unit,
            readonly: bool,
            translations: AppTranslations,
            searchTarget: 'a -> SearchTarget,
            ?searchCaseMatching: CaseMatching,
            ?searchHighlighting: Highlighting
        ) =
        let reactKeyOf x = String.toKebab $"%A{x}"
        let selectedItems, setSelected = React.useState selectedItems
        let filterText, setFilterText = React.useState ""

        let searchConfig = {
            Columns = Set.empty
            CaseMatching = defaultArg searchCaseMatching CaseMatching.CaseInsensitive
            Highlighting = defaultArg searchHighlighting Highlighting.Active
            Term = Option.ofNonNullOrWhitespace filterText
        }

        let search = Searcher(searchConfig, translations)

        let searchedItems =
            items
            |> Seq.map (fun item -> item, formatItem item)
            |> Seq.map (fun (item, text) -> {
                Key = reactKeyOf item
                Text = text
                Value = item
                SearchResult = search.Target(searchTarget item, text)
            })
            |> Seq.sortBy _.Text
            |> Seq.toList

        let toggle item isChecked =
            match isChecked, selectedItems.Contains item with
            | true, true -> () // Already selected
            | true, false -> // Select
                setSelected (selectedItems.Add item)
                onSelect (true, item)

            | false, false -> () // Already unselected
            | false, true -> // Deselect
                setSelected (selectedItems.Remove item)
                onSelect (false, item)

        Daisy.dropdown [
            prop.key key
            prop.className [
                "w-full"
                if readonly then "cursor-default" else "cursor-pointer"
            ]
            prop.children [
                Html.div [
                    prop.key $"%s{key}-content"
                    prop.role "button"
                    prop.tabIndex 0 // Required to open the dropdown
                    prop.className [
                        "input input-bordered w-full"
                        "flex items-center flex-wrap gap-2 h-auto px-1.5 py-2"
                        if readonly then
                            "bg-base-300"
                    ]
                    prop.children [
                        for item in searchedItems do
                            if selectedItems.Contains item.Value then
                                Daisy.badge [
                                    badge.primary
                                    badge.soft
                                    prop.key $"%s{item.Key}-badge"
                                    prop.className "gap-1"
                                    if not readonly then
                                        prop.style [ style.paddingInlineEnd 0 ]
                                    prop.children [
                                        Html.text item.Text
                                        if not readonly then
                                            Daisy.button.button [
                                                button.ghost ++ button.circle
                                                prop.type' "button"
                                                prop.key $"%s{item.Key}-remove-button"
                                                prop.tabIndex -1
                                                prop.className "text-[10px]"
                                                prop.style [ style.custom ("--size", "1.5rem") ]
                                                prop.text "✕"
                                                prop.onMouseDown (fun e -> e.preventDefault ())
                                                prop.onClick (fun _ -> toggle item.Value false)
                                            ]
                                    ]
                                ]

                    ]
                ]

                if items.Count > 0 && not readonly then
                    Daisy.dropdownContent [
                        prop.key $"%s{key}-dropdown-content"
                        prop.tabIndex -1
                        prop.className "menu bg-base-100 rounded-box z-1 w-64 p-0 shadow-sm"
                        prop.children [
                            Html.li [
                                prop.key $"%s{key}-search"
                                prop.className "border-b border-base-200 p-2"
                                prop.children [
                                    Daisy.label.input [
                                        prop.key $"%s{key}-search-input"
                                        prop.className "input input-bordered w-full flex items-center gap-2"
                                        prop.children [
                                            icon fa6Solid.magnifyingGlass
                                            Html.input [
                                                prop.key $"%s{key}-search-input-field"
                                                prop.type' "text"
                                                prop.className "grow bg-transparent outline-none"
                                                prop.placeholder translations.Home.Search
                                                prop.value filterText
                                                prop.onChange setFilterText
                                            ]
                                            if filterText <> "" then
                                                Daisy.button.button [
                                                    button.ghost ++ button.circle ++ button.sm
                                                    prop.key $"%s{key}-clear-search"
                                                    prop.type' "button"
                                                    prop.onMouseDown (fun ev ->
                                                        ev.preventDefault ()
                                                        ev.stopPropagation ()
                                                    )
                                                    prop.onClick (fun ev ->
                                                        ev.preventDefault ()
                                                        ev.stopPropagation ()
                                                        setFilterText ""
                                                    )
                                                    prop.text "✕"
                                                ]
                                        ]
                                    ]
                                ]
                            ]

                            // Select All/None checkbox
                            if items.Count > 1 then
                                Html.li [
                                    prop.key $"%s{key}-select-all"
                                    prop.className "text-primary border-b border-base-200"
                                    prop.children [
                                        Checkbox(
                                            key = $"%s{key}-select-all-checkbox",
                                            state =
                                                (match selectedItems.Count, items.Count with
                                                 | 0, _ -> CheckboxState.NotChecked
                                                 | n, p when n = p -> CheckboxState.Checked
                                                 | _ -> CheckboxState.Indeterminate),
                                            children = [ // ↩
                                                Html.text $"%s{translations.Home.SelectedPlural} (%d{selectedItems.Count})"
                                            ],
                                            onCheck =
                                                fun isChecked ->
                                                    if isChecked then
                                                        // Select all
                                                        selectedItems |> Seq.iter (fun item -> onSelect (true, item))
                                                        setSelected items
                                                    else
                                                        // Deselect all
                                                        selectedItems |> Seq.iter (fun item -> onSelect (false, item))
                                                        setSelected Set.empty
                                        )
                                    ]
                                ]

                            for item in searchedItems do
                                Html.li [
                                    prop.key $"%s{key}-select-%s{item.Key}"
                                    prop.className "hover:bg-base-200"
                                    prop.children [
                                        Checkbox(
                                            key = $"%s{key}-select-%s{item.Key}-checkbox",
                                            state = CheckboxState.CheckedIf(selectedItems.Contains item.Value),
                                            children = [
                                                item.SearchResult |> Highlight.matches Html.span [ prop.key $"%s{key}-select-%s{item.Key}-label" ]
                                            ],
                                            onCheck = toggle item.Value
                                        )
                                    ]
                                ]
                        ]
                    ]
            ]
        ]
