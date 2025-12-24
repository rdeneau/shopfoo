module Shopfoo.Client.Pages.Product.Details

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Pages.Product
open Shopfoo.Client.Pages.Product.Actions
open Shopfoo.Client.Pages.Product.CatalogInfo
open Shopfoo.Client.Pages.Product.ModifyPrice
open Shopfoo.Client.Pages.Shared
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Remoting

type private Msg =
    | CloseDrawer
    | OpenDrawer of Drawer

type private Model = { Drawer: Drawer option }

let private init () = { Drawer = None }, Cmd.none

let private update msg (model: Model) =
    match msg with
    | CloseDrawer -> { model with Drawer = None }, Cmd.none
    | OpenDrawer drawer -> { model with Drawer = Some drawer }, Cmd.none

[<ReactComponent>]
let DetailsView (fullContext: FullContext, sku, fillTranslations, onSave: Toast -> unit) =
    let model, dispatch = React.useElmish (init, update)

    let drawerControl =
        DrawerControl( // ↩
            open' = (fun drawer -> dispatch (OpenDrawer drawer)),
            close = (fun () -> dispatch CloseDrawer)
        )

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

    let isDrawerOpen = model.Drawer.IsSome
    let onSavePrice (price, error) = onSave (Toast.Prices(price, error))
    let onSaveProduct (product, error) = onSave (Toast.Product(product, error))

    let key = "product-details"

    Daisy.drawer [
        prop.key $"%s{key}-drawer"
        drawer.end'
        prop.children [
            Daisy.drawerToggle [
                prop.key $"%s{key}-drawer-toggle"
                prop.id $"%s{key}-drawer-toggle"
                prop.isChecked isDrawerOpen
                prop.onChange ignore<bool> // `onChange` is needed by React because it's a controlled input.
            ]
            Daisy.drawerContent [
                prop.key $"%s{key}-drawer-content"
                if hasActions then
                    prop.className "grid grid-cols-4 gap-4"

                prop.children [
                    Html.div [
                        prop.key $"%s{key}-catalog"
                        prop.children [ CatalogInfoForm "catalog-info" fullContext sku fillTranslations onSaveProduct ]
                        match hasActions, isDrawerOpen with
                        | true, false -> prop.className "col-span-3"
                        | true, true -> prop.className "col-span-2"
                        | _ -> ()
                    ]

                    if hasActions then
                        Html.div [
                            prop.key $"%s{key}-actions"
                            prop.className "col-span-1"
                            prop.children [ ActionsForm "actions" fullContext sku drawerControl onSavePrice ]
                        ]
                ]
            ]
            Daisy.drawerSide [
                prop.key $"%s{key}-drawer-side"
                prop.className "h-full w-full z-[9999]"
                prop.children [
                    Daisy.drawerOverlay [ // ↩
                        prop.key $"%s{key}-drawer-overlay"
                        prop.onClick (fun _ -> drawerControl.Close())
                    ]
                    Html.div [
                        prop.key $"%s{key}-drawer-side-content"
                        prop.className "h-full w-1/4 bg-base-100 p-4"
                        prop.children [
                            match model.Drawer with
                            | None -> ()
                            | Some(ModifyPrice(priceModel, prices)) -> ModifyPriceForm key fullContext priceModel prices drawerControl onSavePrice
                            | Some DefineListPrice
                            | Some InputSales
                            | Some ReceivePurchasedProducts
                            | Some AdjustStockAfterInventory ->
                                // TODO: [Drawer] other actions
                                Html.text "🚧 TODO"
                        ]
                    ]
                ]
            ]
        ]
    ]