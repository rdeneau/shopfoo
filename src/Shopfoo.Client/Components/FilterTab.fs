namespace Shopfoo.Client.Components

open Feliz
open Feliz.DaisyUI
open Glutinum.Iconify
open Shopfoo.Client.Components.Icon

[<RequireQualifiedAccess>]
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
            onSelect: 'item -> unit,
            onReset: unit -> unit
        ) =
        match selectedItem with
        | None ->
            // STATE: OFF - Show Dropdown
            Daisy.dropdown [
                dropdown.hover
                prop.key $"%s{key}-dropdown"
                prop.children [
                    Daisy.button.a [
                        button.link
                        prop.key $"%s{key}-dropdown-button"
                        prop.className "tab h-full gap-2 !font-normal !no-underline" // Force tab styling
                        prop.children [
                            match iconifyIcon with
                            | None -> ()
                            | Some iconifyIcon -> icon iconifyIcon
                            Html.text label
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
                                            prop.onClick (fun _ -> onSelect item)
                                        ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]

        | Some selectedItem ->
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
                        prop.onClick (fun _ -> onReset ())
                        prop.text "✕"
                    ]
                ]
            ]