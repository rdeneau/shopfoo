namespace Shopfoo.Client.Components

open System
open Fable.Core
open Feliz
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Glutinum.Iconify
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Search
open Shopfoo.Common
open Shopfoo.Shared.Translations
open type Shopfoo.Client.Components.Checkbox

type AfterSearch = AfterSearch of shouldClearTerm: bool

type private AfterSearchCallbacks = { ClearTerm: unit -> unit }

type private SearchRunner(afterSearch: AfterSearchCallbacks) =
    member _.Run onSearch =
        match onSearch () with
        | AfterSearch(shouldClearTerm = true) -> afterSearch.ClearTerm()
        | _ -> ()

type SearchInfo = { SearchTerm: string; NoExactMatches: bool }

[<RequireQualifiedAccess>]
module SearchInfo =
    let empty = { SearchTerm = ""; NoExactMatches = false }

[<RequireQualifiedAccess>]
type SearchCompletionStatus =
    | Success
    | Info of message: string
    | Error of message: string

[<RequireQualifiedAccess>]
type SearchButtonProps =
    | None
    | Active of icon: IconifyIcon * tooltip: string * onSearch: (unit -> AfterSearch)
    | Searching
    | SearchComplete of status: SearchCompletionStatus

type SelectItem<'a> = {
    Value: 'a
    Image: ReactElement option
    Text: string
    Selected: bool
    Filterable: bool
    SearchTarget: SearchTarget
}

type private SearchedItem<'a> = {
    Key: string
    Item: SelectItem<'a>
    SearchResult: SearchTargetResult
} with
    member this.HasMatches = this.SearchResult.Matches |> List.exists (fun m -> m.MatchType = TextMatch)

[<Erase>]
type MultiSelect =
    static member private searchButton(key, props: SearchButtonProps, search: SearchRunner) =
        match props with
        | SearchButtonProps.None -> Html.none
        | _ ->
            Daisy.button.button [
                button.ghost ++ button.circle ++ button.sm
                prop.key $"search-input-button-%s{key}"
                prop.type' "button"
                prop.onMouseDown (fun ev ->
                    ev.preventDefault ()
                    ev.stopPropagation ()
                )

                match props with
                | SearchButtonProps.Active(iconifyIcon, tooltip, onSearch) ->
                    prop.title tooltip

                    prop.onClick (fun ev ->
                        ev.preventDefault ()
                        ev.stopPropagation ()
                        search.Run(onSearch)
                    )

                    prop.children [ icon iconifyIcon ]

                | SearchButtonProps.Searching ->
                    prop.children [
                        Daisy.loading [
                            loading.spinner
                            loading.xs
                            prop.key $"search-input-%s{key}-spinner"
                        ]
                    ]

                | SearchButtonProps.SearchComplete(SearchCompletionStatus.Error message) ->
                    prop.title message
                    prop.className "text-error"
                    prop.children [ icon fa6Solid.circleXmark ]

                | SearchButtonProps.SearchComplete(SearchCompletionStatus.Info message) ->
                    prop.title message
                    prop.className "text-info"
                    prop.children [ icon fa6Solid.info ]

                | SearchButtonProps.SearchComplete SearchCompletionStatus.Success ->
                    prop.className "text-success"
                    prop.children [ icon fa6Solid.circleCheck ]

                | SearchButtonProps.None -> ()
            ]

    [<ReactComponent>]
    static member MultiSelect
        (
            key: string, // ↩
            items: SelectItem<'a> list,
            onSearchTermChange: SearchInfo -> unit,
            onSelect: bool * 'a -> unit,
            readonly: bool,
            translations: AppTranslations,
            ?searchCaseMatching: CaseMatching,
            ?searchHighlighting: Highlighting,
            ?searchMoreButtonProps: SearchButtonProps
        ) =
        let reactKeyOf x = String.toKebab $"%A{x}"
        let filterText, setFilterText = React.useState ""
        let searchInputRef = React.useInputRef ()

        let focusSearchInput _ =
            fun () -> searchInputRef.current |> Option.iter _.focus()
            |> JS.runAfter (TimeSpan.FromMilliseconds 150)

        let searchConfig = {
            Columns = Set.empty
            CaseMatching = defaultArg searchCaseMatching CaseMatching.CaseInsensitive
            Highlighting = defaultArg searchHighlighting Highlighting.Active
            Term = Option.ofNonNullOrWhitespace filterText
        }

        let search = Searcher(searchConfig, translations)

        let searchedItems =
            items
            |> Seq.map (fun item -> {
                Key = reactKeyOf item.Value
                Item = item
                SearchResult = search.Target(item.SearchTarget, item.Text)
            })
            |> Seq.sortBy _.Item.Text.ToLower()
            |> Seq.toArray

        let noExactMatches, visibleItems =
            match filterText with
            | String.NullOrWhiteSpace -> false, searchedItems
            | _ ->
                searchedItems |> Array.forall (fun x -> x.Item.Text.ToLower() <> filterText.ToLower()),
                searchedItems |> Array.filter (fun x -> x.Item.Selected || x.HasMatches || not x.Item.Filterable)

        let clearSearchTerm () =
            setFilterText ""
            onSearchTermChange SearchInfo.empty

        let searchRunner = SearchRunner({ ClearTerm = clearSearchTerm })
        let searchButton key props = MultiSelect.searchButton (key, props, searchRunner)

        let searchMoreButton =
            searchMoreButtonProps
            |> Option.map (fun props -> props, searchButton $"%s{key}-search-more-btn" props)

        let selectedValues =
            Set [
                for item in items do
                    if item.Selected then
                        item.Value
            ]

        let toggle value isChecked =
            match isChecked, selectedValues.Contains value with
            | true, true -> () // Already selected
            | true, false -> // Select
                onSelect (true, value)

            | false, false -> () // Already unselected
            | false, true -> // Deselect
                onSelect (false, value)

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
                        "flex items-center flex-wrap gap-2 h-auto min-h-11 px-1.5 py-2"
                        if readonly then
                            "bg-base-300"
                    ]
                    prop.onFocus focusSearchInput
                    prop.children [
                        for { Key = key; Item = item } in searchedItems do
                            if item.Selected then
                                Daisy.badge [
                                    badge.primary
                                    badge.soft
                                    prop.key $"%s{key}-badge"
                                    prop.className "gap-1"
                                    if not readonly then
                                        prop.style [ style.paddingInlineEnd 0 ]
                                    prop.children [
                                        Html.text item.Text
                                        if not readonly then
                                            Daisy.button.button [
                                                button.ghost ++ button.circle
                                                prop.type' "button"
                                                prop.key $"%s{key}-remove-button"
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

                if searchedItems.Length > 0 && not readonly then
                    Daisy.dropdownContent [
                        prop.key $"%s{key}-dropdown-content"
                        prop.tabIndex -1
                        prop.className "bg-base-100 rounded-box z-1 w-64 p-0 shadow-sm"
                        prop.children [
                            // Search input
                            Html.li [
                                prop.key $"%s{key}-search"
                                prop.className "p-2"
                                prop.children [
                                    Daisy.label.input [
                                        prop.key $"%s{key}-search-input"
                                        prop.className "input input-bordered w-full flex items-center gap-2"
                                        prop.children [
                                            icon fa6Solid.magnifyingGlass
                                            Html.input [
                                                prop.ref searchInputRef
                                                prop.key $"%s{key}-search-input-field"
                                                prop.type' "text"
                                                prop.autoFocus true // Not working but left for documentation purpose
                                                prop.className "grow bg-transparent outline-none"
                                                prop.placeholder translations.Home.Search
                                                prop.value filterText
                                                prop.onChange (fun searchTerm ->
                                                    setFilterText searchTerm
                                                    onSearchTermChange { SearchTerm = searchTerm; NoExactMatches = noExactMatches }
                                                )

                                                match searchMoreButton with
                                                | Some(SearchButtonProps.Active(_, _, onSearch), _) ->
                                                    prop.onKeyDown (
                                                        Feliz.key.enter,
                                                        fun ev ->
                                                            ev.preventDefault ()
                                                            searchRunner.Run(onSearch)
                                                    )
                                                | _ -> ()
                                            ]

                                            match searchMoreButton with
                                            | Some(_, button) -> button
                                            | None -> ()

                                            if filterText <> "" then
                                                searchButton $"%s{key}-clear-search-btn"
                                                <| SearchButtonProps.Active(
                                                    icon = fa6Solid.xmark,
                                                    tooltip = translations.Home.Clear,
                                                    onSearch = fun _ -> AfterSearch(shouldClearTerm = true)
                                                )
                                        ]
                                    ]
                                ]
                            ]

                            // Select All/None checkbox
                            if searchedItems.Length > 1 then
                                Html.li [
                                    prop.key $"%s{key}-select-all"
                                    prop.className "text-primary border-b border-base-200"
                                    prop.children [
                                        Checkbox(
                                            key = $"%s{key}-select-all-checkbox",
                                            state =
                                                (match selectedValues.Count, searchedItems.Length with
                                                 | 0, _ -> CheckboxState.NotChecked
                                                 | n, p when n = p -> CheckboxState.Checked
                                                 | _ -> CheckboxState.Indeterminate),
                                            children = [ // ↩
                                                Html.text $"%s{translations.Home.SelectedPlural} (%d{selectedValues.Count})"
                                            ],
                                            onCheck =
                                                fun isChecked ->
                                                    if isChecked then
                                                        // Select all visible
                                                        visibleItems
                                                        |> Seq.iter (fun { Item = item } ->
                                                            if not item.Selected then
                                                                onSelect (true, item.Value)
                                                        )
                                                    else
                                                        // Deselect all
                                                        selectedValues |> Seq.iter (fun value -> onSelect (false, value))
                                        )
                                    ]
                                ]

                            for item in visibleItems do
                                Html.li [
                                    prop.key $"%s{key}-select-%s{item.Key}"
                                    prop.className "group flex items-center hover:bg-base-300 transition-colors"
                                    prop.children [
                                        Checkbox(
                                            key = $"%s{key}-select-%s{item.Key}-checkbox",
                                            state = CheckboxState.CheckedIf(item.Item.Selected),
                                            children = [
                                                item.SearchResult |> Highlight.matches Html.span [ prop.key $"%s{key}-select-%s{item.Key}-label" ]
                                            ],
                                            onCheck = toggle item.Item.Value
                                        )
                                        match item.Item.Image with
                                        | Some img -> img
                                        | None -> ()
                                    ]
                                ]
                        ]
                    ]
            ]
        ]