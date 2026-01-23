namespace Shopfoo.Client.Components

open Fable.Core
open Feliz
open Feliz.DaisyUI
open Glutinum.Iconify
open Shopfoo.Client
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Routing

type FilterAction<'item> =
    | Click of onClick: ('item -> unit)
    | NavigateToPage of getPage: ('item -> Page)

[<Erase; RequireQualifiedAccess>]
type Filter =
    [<ReactComponent>]
    static member FilterTab
        (
            key: string, // ↩
            label: string,
            iconifyIcon: IconifyIcon option,
            items: 'item list,
            selectedItem: 'item option,
            formatItem: 'item -> string,
            onSelect: FilterAction<'item>,
            onReset: FilterAction<unit>
        ) =
        match items, selectedItem with
        | [], _ -> Html.none
        | _, None ->
            // STATE: OFF - Show Dropdown
            Daisy.dropdown [
                dropdown.hover
                prop.key $"%s{key}-dropdown"
                prop.children [
                    Daisy.button.a [
                        button.link
                        prop.key $"%s{key}-dropdown-button"
                        prop.className "tab h-full !font-normal !no-underline" // Force tab styling
                        prop.children [
                            Daisy.indicator [
                                prop.key $"%s{key}-dropdown-indicator"
                                prop.children [
                                    Daisy.indicatorItem [
                                        prop.key $"%s{key}-dropdown-badge"
                                        prop.className "badge badge-sm badge-primary badge-soft px-1"
                                        prop.text items.Length
                                    ]

                                    Html.div [
                                        prop.key $"%s{key}-dropdown-button-content"
                                        prop.className "flex items-center gap-2 pr-3"
                                        prop.children [
                                            match iconifyIcon with
                                            | None -> ()
                                            | Some iconifyIcon -> icon iconifyIcon
                                            Html.text label
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Daisy.dropdownContent [
                        prop.key $"%s{key}-dropdown-content"
                        prop.className "menu p-2 shadow bg-base-100 rounded-box w-52"
                        prop.children [
                            for item in items ->
                                let itemLabel = formatItem item

                                Html.li [
                                    prop.key $"%s{key}-menu-item-%s{itemLabel}-li"
                                    prop.children [
                                        Html.a [
                                            prop.key $"%s{key}-menu-item-%s{itemLabel}-a"
                                            prop.text itemLabel
                                            match onSelect with
                                            | Click onClick -> prop.onClick (fun _ -> onClick item)
                                            | NavigateToPage getPage -> yield! prop.hrefRouted (getPage item)
                                        ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]

        | _, Some selectedItem ->
            // STATE: ON - Show Active Filter with Reset
            Html.div [
                prop.key $"%s{key}-tab"
                prop.className "tab tab-active gap-2 !px-3 before:!w-[calc(100%-26px)] before:!left-[10px]"
                prop.children [
                    match iconifyIcon with
                    | None -> ()
                    | Some iconifyIcon -> icon iconifyIcon
                    Html.text (formatItem selectedItem)
                    Daisy.button.a [
                        prop.key $"%s{key}-tab-close-button"
                        prop.className "btn btn-ghost btn-sm btn-circle ml-[-4px]"
                        prop.text "✕"
                        match onReset with
                        | Click onClick -> prop.onClick (fun _ -> onClick ())
                        | NavigateToPage getPage -> yield! prop.hrefRouted (getPage ())
                    ]
                ]
            ]