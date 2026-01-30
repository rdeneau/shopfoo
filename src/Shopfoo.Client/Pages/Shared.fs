module Shopfoo.Client.Pages.Shared

open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Remoting

[<RequireQualifiedAccess>]
type Toast =
    | Lang of Lang
    | Prices of Prices * ApiError option
    | Product of Product * ApiError option
    | Stock of Stock * ApiError option

/// Provides access for pages to main `AppView` component functionality:
/// - Shared data access (e.g. `FullContext`) that pages should not stored in their own model
///   to ensure a single source of truth and prevent discrepancies or complex synchronization mechanisms.
/// - Shared data update: e.g. `FillTranslations` to update translations cached in the `FullContext`.
/// - Shared actions: e.g. `LoginUser` to update the current user in the main app context.
///
/// Follows the SOLID principles below:
/// - Dependency Inversion Principle: Pages depend on abstractions (`Env` interfaces) rather than concrete implementations.
///   The `AppView` component provides the implementation to the pages that remain independent of it thanks to the declaration order.
/// - Interface Segregation Principle: Each interface in the `Env` module is focused on a specific functionality.
///   This allow the page to depend only on what they need.
///
/// Based on a technique described in this article written by Vladimir Shchur: \
/// 🔗 [Dependency injection in F#. The missing manual.](https://medium.com/@lanayx/dependency-injection-in-f-the-missing-manual-d376e9cafd0f)
[<RequireQualifiedAccess>]
module Env =
    type IFullContext =
        abstract member FullContext: FullContext

    type IFillTranslations =
        abstract member FillTranslations: translations: Translations -> unit

    type ILoginUser =
        abstract member LoginUser: user: User -> unit

    type IShowToast =
        abstract member ShowToast: toast: Toast -> unit

    let fillTranslations (env: #IFillTranslations) translations = env.FillTranslations translations
    let prepareQueryWithTranslations (env: #IFullContext) query = env.FullContext.PrepareQueryWithTranslations query
    let user (env: #IFullContext) = env.FullContext.User

[<AutoOpen>]
module EnvExtensions =
    type Env.IFullContext with
        member env.Translations = env.FullContext.Translations