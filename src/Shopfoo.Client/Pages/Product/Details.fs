module Shopfoo.Client.Pages.Product.Details

open Feliz
open Shopfoo.Client.Pages.Product.Actions
open Shopfoo.Client.Pages.Product.CatalogInfo
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Remoting

[<ReactComponent>]
let DetailsView (fullContext: FullContext, sku, fillTranslations, onSaveProduct) =
    // Access to the Catalog is required. PageNotFound otherwise.
    React.useEffectOnce (fun () ->
        if not (fullContext.User.CanAccess Feat.Catalog) then
            Router.navigatePage (Page.CurrentNotFound())
    )

    let hasActions =
        match fullContext.User with
        | UserCanAccess Feat.Sales
        | UserCanAccess Feat.Warehouse -> true
        | _ -> false

    Html.section [
        prop.key "product-details-section"
        if hasActions then
            prop.className "grid grid-cols-4 gap-4"

        prop.children [
            Html.div [
                prop.key "product-catalog"
                prop.children [ CatalogInfoForm "catalog-info" fullContext sku fillTranslations onSaveProduct ]
                if hasActions then
                    prop.className "col-span-3"
            ]

            if hasActions then
                Html.div [
                    prop.key "product-actions"
                    prop.className "col-span-1"
                    prop.children [ ActionsForm "actions" fullContext ]
                ]
        ]
    ]