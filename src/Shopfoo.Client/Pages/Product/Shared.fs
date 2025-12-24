namespace Shopfoo.Client.Pages.Product

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales

type PriceModificationIntent =
    | Increase
    | Decrease

type PriceType =
    | ListPrice
    | RetailPrice

type PriceModel = {
    Type: PriceType
    Value: Money
    Intent: PriceModificationIntent
} with
    member this.Update(prices: Prices) =
        match this.Type with
        | ListPrice -> { prices with ListPrice = Some this.Value }
        | RetailPrice -> { prices with RetailPrice = RetailPrice.Regular this.Value }

type Drawer =
    | DefineListPrice
    | DefineRetailPrice
    | ModifyPrice of PriceModel * Prices
    | InputSales
    | ReceivePurchasedProducts
    | AdjustStockAfterInventory

/// This object is created by the view holding the Drawer.
/// It's passed to the components to let them open/close the drawer.
/// As the opening and the closing can be performed in different components,
/// each can be notified of these events by attaching a listener with `x.OnOpen(f)` and `x.OnClose(f)` respectively.
/// The object supports attaching several listeners for both events.
type DrawerControl(open': Drawer -> unit, close: unit -> unit) =
    let onOpenListeners = ResizeArray<Drawer -> unit>()
    let onCloseListeners = ResizeArray<Drawer -> unit>()
    let invokeWith x f = f x
    let mutable openedDrawer = None

    member this.OnOpen(f) = onOpenListeners.Add(f)
    member this.OnClose(f) = onCloseListeners.Add(f)

    member this.Open(drawer) =
        open' drawer
        onOpenListeners |> Seq.iter (invokeWith drawer)
        openedDrawer <- Some drawer

    /// `drawer` is used optionally to specify the new drawer data to listeners, to pass saved values for example.
    member this.Close(?drawer) =
        drawer
        |> Option.orElse openedDrawer
        |> Option.iter (fun drawer ->
            onCloseListeners |> Seq.iter (invokeWith drawer)
            openedDrawer <- None
        )

        close ()