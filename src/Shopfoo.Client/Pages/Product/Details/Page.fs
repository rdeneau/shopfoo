module Shopfoo.Client.Pages.Product.Details.Page

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Pages.Product
open Shopfoo.Client.Pages.Product.Details.Actions
open Shopfoo.Client.Pages.Product.Details.AdjustStock
open Shopfoo.Client.Pages.Product.Details.CatalogInfo
open Shopfoo.Client.Pages.Product.Details.ManagePrice
open Shopfoo.Client.Pages.Shared
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
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
let ProductDetailsView (env: #Env.IFullContext & #Env.IFillTranslations & #Env.IShowToast) sku =
    let model, dispatch = React.useElmish (init, update)
    let productModel, updateProductModel = React.useState { ProductModel.SKU = sku; SoldOut = false }

    let fullContext = env.FullContext

    // Access to the Catalog is required. PageNotFound otherwise.
    React.useEffectOnce (fun () ->
        if not (fullContext.User.CanAccess Feat.Catalog) then
            Router.navigatePage (Page.CurrentNotFound())
    )

    let drawerControl =
        DrawerControl( // ↩
            open' = (fun drawer -> dispatch (OpenDrawer drawer)),
            close = (fun () -> dispatch CloseDrawer)
        )

    let hasActions =
        match fullContext.User, sku.Type with
        | (UserCanAccess Feat.Sales | UserCanAccess Feat.Warehouse), (SKUType.FSID _ | SKUType.ISBN _) -> true
        | _ -> false

    let isDrawerOpen = model.Drawer.IsSome

    let onSavePrice (price, error) = env.ShowToast(Toast.Prices(price, error))
    let onSaveStock (stock, error) = env.ShowToast(Toast.Stock(stock, error))

    let onSaveProduct (product: Product, error) =
        updateProductModel { productModel with SKU = product.SKU }
        env.ShowToast(Toast.Product(product, error))

    let setSoldOut soldOut =
        if soldOut <> productModel.SoldOut then
            updateProductModel { productModel with SoldOut = soldOut }

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
                        prop.children [ CatalogInfoForm "catalog-info" fullContext productModel env.FillTranslations onSaveProduct ]
                        match hasActions, isDrawerOpen with
                        | true, false -> prop.className "col-span-3"
                        | true, true -> prop.className "col-span-2"
                        | _ -> ()
                    ]

                    if hasActions then
                        Html.div [
                            prop.key $"%s{key}-actions"
                            prop.className "col-span-1"
                            prop.children [ ActionsForm "actions" fullContext sku drawerControl onSavePrice setSoldOut ]
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
                            | Some(ManagePrice(priceModel, prices)) -> ManagePriceForm key fullContext priceModel prices drawerControl onSavePrice
                            | Some(AdjustStockAfterInventory stock) -> AdjustStockForm key fullContext stock drawerControl onSaveStock
                            | Some InputSales
                            | Some ReceivePurchasedProducts ->
                                // TODO: [Drawer] other actions
                                Html.text "🚧 TODO"
                        ]
                    ]
                ]
            ]
        ]
    ]