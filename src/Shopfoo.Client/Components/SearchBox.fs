namespace Shopfoo.Client.Components

open Fable.Core
open Feliz
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Glutinum.Iconify
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client.Components.Icon
open Shopfoo.Shared.Translations

type AfterSearch = AfterSearch of shouldClearTerm: bool

type private AfterSearchCallbacks = { ClearTerm: unit -> unit }

type private SearchRunner(afterSearch: AfterSearchCallbacks) =
    member _.Run onSearch =
        match onSearch () with
        | AfterSearch(shouldClearTerm = true) -> afterSearch.ClearTerm()
        | _ -> ()

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

[<Erase>]
type SearchBox =
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
    static member SearchBox
        (
            key: string,
            value: string,
            onChange: string -> unit,
            translations: AppTranslations,
            ?inputRef: IRefValue<Browser.Types.HTMLInputElement option>,
            ?onKeyDown: Browser.Types.KeyboardEvent -> unit,
            ?searchMoreButtonProps: SearchButtonProps,
            ?className: string
        ) =
        let clearSearchTerm () = onChange ""
        let searchRunner = SearchRunner { ClearTerm = clearSearchTerm }
        let searchButton key props = SearchBox.searchButton (key, props, searchRunner)

        Daisy.label.input [
            prop.key key
            prop.className [
                "input input-bordered flex items-center gap-2"
                match className with
                | Some c -> c
                | None -> "w-full"
            ]
            prop.children [
                icon fa6Solid.magnifyingGlass
                Html.input [
                    prop.key $"{key}-input"
                    prop.type' "text"
                    prop.className "grow bg-transparent outline-none"
                    prop.value value
                    prop.onChange onChange

                    match searchMoreButtonProps with
                    | Some _ -> prop.placeholder translations.Home.SearchOrAdd
                    | None -> prop.placeholder translations.Home.Search

                    match inputRef with
                    | Some ref -> prop.ref ref
                    | None -> ()

                    match onKeyDown with
                    | Some handler -> prop.onKeyDown handler
                    | None -> ()
                ]

                match searchMoreButtonProps with
                | Some props -> searchButton $"{key}-search-more-btn" props
                | None -> ()

                if value <> "" then
                    Daisy.button.button [
                        button.ghost ++ button.circle ++ button.sm
                        prop.key $"{key}-clear-btn"
                        prop.type' "button"
                        prop.title translations.Home.Clear
                        prop.onClick (fun _ -> clearSearchTerm ())
                        prop.onMouseDown (fun ev ->
                            ev.preventDefault ()
                            ev.stopPropagation ()
                        )
                        prop.children [ icon fa6Solid.xmark ]
                    ]
            ]
        ]