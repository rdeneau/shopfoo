module Shopfoo.Client.Components.Icon

open Feliz.Iconify
open type Feliz.Iconify.Offline.Exports
open Shopfoo.Client.UI

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
let icon (iconifyIcon: Glutinum.Iconify.IconifyIcon) =
    Icon [ icon.icon iconifyIcon ] |> React.withKeyAuto