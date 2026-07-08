module Shopfoo.Client.Components.Icon

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Glutinum.Iconify
open Shopfoo.Client.UI

module private IconifyReact =
    [<Import("Icon", "@iconify/react")>]
    let iconComponent: obj = jsNative

type Rotate =
    | Deg90
    | Deg180
    | Deg270

// https://iconify.design/docs/iconify-icon/transform.html
type IconProp =
    | IconifyIcon of IconifyIcon
    | HFlip
    | VFlip
    | Rotate of Rotate

    /// Specify both width and height in CSS units using `length.xxx` Feliz helpers.
    | Size of Feliz.Styles.ICssUnit

/// <summary>
/// Helper to create an icon component with the given <c>props</c>.
/// </summary>
/// <example>
/// <code>
/// open Glutinum.IconifyIcons.Fa6Solid
/// open Shopfoo.Client.Components.Icon
///
/// let myIconComponent = iconCustom [ IconifyIcon fa6Solid.brush; Rotate Deg90; Size (length.rem 2.0) ]
/// </code>
/// </example>
let iconCustom (props: IconProp list) =
    let iconProps =
        props
        |> List.choose (function
            | IconifyIcon iconifyIcon -> Some("icon" ==> iconifyIcon)
            | HFlip -> Some("hFlip" ==> true)
            | VFlip -> Some("vFlip" ==> true)
            | Rotate Deg90 -> Some("rotate" ==> "90deg")
            | Rotate Deg180 -> Some("rotate" ==> "180deg")
            | Rotate Deg270 -> Some("rotate" ==> "270deg")
            | Size size ->
                let value = string size
                Some("width" ==> value))
        |> createObj

    ReactLegacy.createElement(unbox<ReactNode> IconifyReact.iconComponent, iconProps)
    |> React.withKeyAuto

/// <summary>
/// Helper to create an icon component with the given <c>iconifyIcon</c>.
/// </summary>
/// <example>
/// <code>
/// open Glutinum.IconifyIcons.Fa6Solid
/// open Shopfoo.Client.Components.Icon
///
/// let myIconComponent = icon fa6Solid.brush
/// </code>
/// </example>
let icon (iconifyIcon: IconifyIcon) = iconCustom [ IconifyIcon iconifyIcon ]
