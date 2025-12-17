module Shopfoo.Client.Pages.Product.Details

open Feliz
open Shopfoo.Client.Pages.Product.Actions
open Shopfoo.Client.Pages.Product.CatalogInfo
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let DetailsView (fullContext: FullContext, sku, fillTranslations, onSaveProduct) =
    match fullContext.User with
    | UserCanNotAccess Feat.Catalog ->
        React.useEffectOnce (fun () -> Router.navigatePage (Page.CurrentNotFound()))
        Html.none
    | _ ->
        Html.section [
            prop.key "product-details-section"
            prop.className "grid grid-cols-4 gap-4"
            prop.children [
                Html.div [
                    prop.key "product-catalog"
                    prop.className "col-span-3"
                    prop.children [ CatalogInfoForm "catalog-info" fullContext sku fillTranslations onSaveProduct ]
                ]
                Html.div [
                    prop.key "product-actions"
                    prop.className "col-span-1"
                    prop.children [ ActionsForm "actions" fullContext ]
                ]
            ]
        ]