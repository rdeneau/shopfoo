module Shopfoo.Client.Components.Icon

open Feliz.Iconify
open type Feliz.Iconify.Offline.Exports
open Shopfoo.Client.UI

type Rotate =
    | Deg90
    | Deg180
    | Deg270

// https://iconify.design/docs/iconify-icon/transform.html
type IconProp =
    | IconifyIcon of Glutinum.Iconify.IconifyIcon
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
    Icon [
        for prop in props do
            match prop with
            | IconifyIcon iconifyIcon -> icon.icon iconifyIcon
            | HFlip -> icon.hFlip true
            | VFlip -> icon.vFlip true
            | Rotate Deg90 -> icon.rotate "deg90"
            | Rotate Deg180 -> icon.rotate "deg180"
            | Rotate Deg270 -> icon.rotate "deg270"
            | Size size -> icon.width (string size)
    ]
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
let icon (iconifyIcon: Glutinum.Iconify.IconifyIcon) = iconCustom [ IconifyIcon iconifyIcon ]