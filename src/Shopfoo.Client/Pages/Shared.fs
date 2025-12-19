module Shopfoo.Client.Pages.Shared

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Shared.Errors

[<RequireQualifiedAccess>]
type Toast =
    | Lang of Lang
    | Prices of Prices * ApiError option
    | Product of Product * ApiError option