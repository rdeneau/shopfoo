module Shopfoo.Client.Pages.Product.Details.Actions

open System
open Browser.Types
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Glutinum.IconifyIcons.Fa6Solid
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Actions
open Shopfoo.Client.Components.Dialog
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Pages.Product
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type private Action =
    | None
    | MarkAsSoldOut
    | WarnMarkAsSoldOutForbidden
    | RemoveListPrice
    | SavePrices

type private Dialog = { Action: Action }

type private Model = {
    Prices: Remote<Prices>
    PriceActionStatus: Action * Remote<DateTime>
    Stock: Remote<Stock>
    PurchasePriceStats: Remote<PurchasePrices>
}

type private Msg =
    | PricesFetched of ApiResult<GetPricesResponse>
    | PurchasePricestatsFetched of ApiResult<GetPurchasePricesResponse>
    | StockFetched of ApiResult<Stock>
    | PerformAction of Action * SKU * ApiCall<unit>

[<RequireQualifiedAccess>]
module private Cmd =
    let determineStock (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.DetermineStock request
            Error = Error >> StockFetched
            Success = Ok >> StockFetched
        }

    let loadPrices (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.GetPrices request
            Error = Error >> PricesFetched
            Success = Ok >> PricesFetched
        }

    let loadPurchasePrices (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.GetPurchasePrices request
            Error = Error >> PurchasePricestatsFetched
            Success = Ok >> PurchasePricestatsFetched
        }

    let perform action (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call =
                fun api ->
                    match action with
                    | Action.RemoveListPrice -> api.Prices.RemoveListPrice request
                    | Action.MarkAsSoldOut -> api.Prices.MarkAsSoldOut request
                    | _ ->
                        // TODO: handle unexpected state properly: modal "Oops, something went wrong" + cmd to log the error
                        async {
                            // This action should not happen here. It's handled as an error from the server side for simplicity’s sake.
                            return Error(ServerError.ApiError(ApiError.Technical($"Unexpected action: %A{action}")))
                        }
            Error = fun apiError -> PerformAction(action, request.Body.SKU, Done(Error apiError))
            Success = fun data -> PerformAction(action, request.Body.SKU, Done(Ok data))
        }

let private init (fullContext: FullContext) sku =
    {
        Prices = Remote.Loading
        PriceActionStatus = Action.None, Remote.Empty
        Stock = Remote.Loading
        PurchasePriceStats = Remote.Loading
    },
    Cmd.batch [ // ↩
        Cmd.loadPrices (fullContext.PrepareRequest sku)
        Cmd.determineStock (fullContext.PrepareRequest sku)
        Cmd.loadPurchasePrices (fullContext.PrepareRequest sku)
    ]

let private update (fullContext: FullContext) onSavePrice (msg: Msg) (model: Model) =
    match msg with
    | PricesFetched(Ok response) -> { model with Prices = response.Prices |> Remote.ofOption }, Cmd.none
    | PricesFetched(Error apiError) -> { model with Prices = Remote.LoadError apiError }, Cmd.none

    | PurchasePricestatsFetched(Ok data) -> { model with PurchasePriceStats = Remote.Loaded data.Stats }, Cmd.none
    | PurchasePricestatsFetched(Error apiError) -> { model with PurchasePriceStats = Remote.LoadError apiError }, Cmd.none

    | StockFetched(Ok stock) -> { model with Stock = Remote.Loaded stock }, Cmd.none
    | StockFetched(Error apiError) -> { model with Stock = Remote.LoadError apiError }, Cmd.none

    | PerformAction(action, sku, Start) ->
        { model with PriceActionStatus = action, Remote.Loading }, // ↩
        Cmd.perform action (fullContext.PrepareRequest { SKU = sku })

    | PerformAction(action, _, Done result) ->
        let prices =
            match model.Prices, action with
            | Remote.Loaded prices, Action.MarkAsSoldOut -> Some { prices with RetailPrice = RetailPrice.SoldOut }
            | Remote.Loaded prices, Action.RemoveListPrice -> Some { prices with ListPrice = None }
            | _ -> None

        match prices with
        | Some prices ->
            { model with Prices = Remote.Loaded prices; PriceActionStatus = action, result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult },
            Cmd.ofEffect (fun _ -> onSavePrice (prices, result |> Result.tryGetError))
        | _ ->
            // Unexpected case
            // TODO: report it to the users, inviting them to reload the page
            // TODO: send a report to the server
            model, Cmd.none

[<ReactComponent>]
let ActionsForm key fullContext sku (drawerControl: DrawerControl) onSavePrice setSoldOut =
    let model, dispatch = React.useElmish (init fullContext sku, update fullContext onSavePrice, [||])

    React.useEffect (
        (fun () ->
            match model.Prices with
            | Remote.Loaded prices -> setSoldOut (prices.RetailPrice = RetailPrice.SoldOut)
            | _ -> ()
        ),
        [| box model.Prices |]
    )

    let translations = fullContext.Translations

    // As the drawers are opened from dropdown menus that are positioned above the side drawer,
    // we apply two fixing strategies:
    // 1. Blur the menu to hide it (on mouse out only?).
    // 2. Set a high z-index to the side drawer (see z-[9999] below).
    drawerControl.OnOpen(fun _ -> JS.blurActiveElement ())

    // When the drawer is closed after a price modification, we refresh the prices.
    drawerControl.OnClose(fun drawer ->
        match drawer with
        | Drawer.AdjustStockAfterInventory stock -> dispatch (StockFetched(Ok stock))
        | Drawer.ManagePrice(_, savedPrices) -> dispatch (PricesFetched(Ok { Prices = Some savedPrices }))
        | _ -> ()
    )

    let modalRef: IRefValue<HTMLElement option> = React.useElementRef ()
    let dialog, setDialog = React.useState { Dialog.Action = Action.None }

    let updateModal f =
        modalRef.current
        |> Option.iter (
            function
            | :? HTMLDialogElement as dialog -> f dialog
            | _ -> ()
        )

    // Close the modal after success with a short delay (500ms),
    // time to let the user get this result visually.
    React.useEffect (fun () ->
        match model.PriceActionStatus with
        | priceAction, Remote.Loaded _ when priceAction = dialog.Action ->
            JS.runAfter // ↩
                (TimeSpan.FromMilliseconds(500))
                (fun () -> updateModal _.close())
        | _ -> ()
    )

    let openModal action =
        setDialog { Dialog.Action = action }
        updateModal _.showModal()

    let closeModal (e: MouseEvent) =
        e.preventDefault ()
        updateModal _.close()
        setDialog { Dialog.Action = Action.None }

    let confirmButton label =
        Buttons.SaveButton(
            key = $"%s{key}-dialog-confirm-price",
            label = label,
            tooltipOk = translations.Home.Completed,
            tooltipError = (fun err -> translations.Home.Error(err.ErrorMessage)),
            tooltipProps = [ tooltip.left ],
            saveDate = snd model.PriceActionStatus,
            disabled = false,
            onClick = (fun _ -> dispatch (PerformAction(dialog.Action, sku, Start)))
        )

    let dialogProps =
        match dialog.Action with
        | Action.MarkAsSoldOut ->
            let x = translations.Product.PriceAction.MarkAsSoldOutDialog
            DialogProps.Confirmation(translations, x.Question, confirmButton x.Confirm)

        | Action.RemoveListPrice ->
            let x = translations.Product.PriceAction.RemoveListPriceDialog
            DialogProps.Confirmation(translations, x.Question, confirmButton x.Confirm)

        | Action.WarnMarkAsSoldOutForbidden -> // ↩
            DialogProps.Warning(translations, translations.Product.PriceAction.WarnMarkAsSoldOutForbidden)

        | Action.SavePrices
        | Action.None -> DialogProps.Empty

    React.fragment [
        ModalDialog $"%s{key}-dialog" modalRef dialogProps closeModal

        Daisy.fieldset [
            prop.key $"%s{key}-fieldset"
            prop.className "bg-base-200 border border-base-300 rounded-box p-4"
            prop.children [
                Html.legend [
                    prop.key "product-actions-legend"
                    prop.className "text-sm"
                    prop.text $"⚡ %s{translations.Product.Actions}"
                ]

                // -- Prices ----
                match model.Prices, translations with
                | Remote.Empty, _ -> ()
                | Remote.Loading, _
                | _, TranslationsMissing PageCode.Product -> Daisy.skeleton [ prop.className "h-48 w-full"; prop.key "prices-skeleton" ]
                | Remote.LoadError apiError, _ -> Alert.apiError "prices-load-error" apiError fullContext.User
                | Remote.Loaded prices, _ ->
                    // -- ListPrice ----
                    Daisy.fieldsetLabel [ prop.key "list-price-label"; prop.text translations.Product.ListPrice ]

                    ActionsDropdown "list-price" "mb-4" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoneyOptional prices.ListPrice) [
                        match prices.ListPrice with
                        | Some price ->
                            ActionProps.withIcon
                                "increase-list-price"
                                PriceAction.Icons.increase
                                translations.Product.PriceAction.Increase
                                (fun () -> drawerControl.Open(Drawer.ManagePrice(ListPrice.To Increase price, prices)))

                            ActionProps.withIcon
                                "decrease-list-price"
                                PriceAction.Icons.decrease
                                translations.Product.PriceAction.Decrease
                                (fun () -> drawerControl.Open(Drawer.ManagePrice(ListPrice.To Decrease price, prices)))

                            ActionProps.withIcon
                                "remove-list-price"
                                PriceAction.Icons.remove
                                translations.Product.PriceAction.Remove
                                (fun () -> openModal Action.RemoveListPrice)

                        | None ->
                            ActionProps.withIcon
                                "define-list-price"
                                PriceAction.Icons.define
                                translations.Product.PriceAction.Define
                                (fun () -> drawerControl.Open(Drawer.ManagePrice(ListPrice.ToDefine prices.Currency, prices)))
                    ]

                    // -- RetailPrice ----
                    Daisy.fieldsetLabel [
                        prop.key "retail-price-label"
                        prop.children [
                            Html.text translations.Product.RetailPrice
                            match prices.ListPrice, prices.RetailPrice with
                            | Some listPrice, RetailPrice.Regular retailPrice when listPrice > retailPrice ->
                                match Money.tryCompute listPrice retailPrice (fun x y -> round (-100m * (x - y) / x)) with
                                | Some discount ->
                                    Html.div [
                                        prop.key "discount"
                                        prop.className "ml-auto"
                                        prop.text $"%s{translations.Product.Discount}%s{translations.Home.Colon} %.0f{discount.Value}%%"
                                    ]
                                | _ -> ()
                            | _ -> ()
                        ]
                    ]

                    ActionsDropdown "retail-price"  "mb-4" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoneyOptional(prices.RetailPrice.ToOption())) [
                        match prices.RetailPrice with
                        | RetailPrice.Regular price ->
                            ActionProps.withIcon
                                "increase-retail-price"
                                PriceAction.Icons.increase
                                translations.Product.PriceAction.Increase
                                (fun () -> drawerControl.Open(ManagePrice(RetailPrice.To Increase price, prices)))

                            ActionProps.withIcon
                                "decrease-retail-price"
                                PriceAction.Icons.decrease
                                translations.Product.PriceAction.Decrease
                                (fun () -> drawerControl.Open(ManagePrice(RetailPrice.To Decrease price, prices)))

                            match model.Stock with
                            | Remote.Loaded stock ->
                                let action =
                                    if stock.Quantity = 0 then
                                        Action.MarkAsSoldOut
                                    else
                                        Action.WarnMarkAsSoldOutForbidden

                                ActionProps.withIcon
                                    "mark-as-sold-out"
                                    PriceAction.Icons.soldOut
                                    translations.Product.PriceAction.MarkAsSoldOut
                                    (fun () -> openModal action)
                            | _ -> ()

                        | RetailPrice.SoldOut ->
                            ActionProps.withIcon
                                "define-retail-price"
                                PriceAction.Icons.define
                                translations.Product.PriceAction.Define
                                (fun () -> drawerControl.Open(Drawer.ManagePrice(RetailPrice.ToDefine prices.Currency, prices)))
                    ]

                    // -- Purchase Prices ----
                    match prices.RetailPrice, model.PurchasePriceStats with
                    | RetailPrice.Regular retailPrice, Remote.Loaded purchasePrices ->
                        let marginPct purchasedPrice =
                            Money.tryCompute retailPrice purchasedPrice (fun r p -> round (100m * (r - p) / r))
                            |> Option.map _.Value

                        let lastPurchasePrice = purchasePrices.LastPrice |> Option.map fst

                        let lastDate =
                            purchasePrices.LastPrice
                            |> Option.map (fun (_, date) -> {| day = translations.Home.DayInMonthOf date; month = translations.Home.ShortMonthOf date |})
                            |> Option.defaultValue {| day = "-"; month = "" |}

                        // -- Last Purchase Price ----
                        Daisy.fieldsetLabel [
                            prop.key "last-purchase-price-label"
                            prop.children [
                                Html.text (translations.Product.LastPurchasePrice(day = lastDate.day, month = lastDate.month))
                                match lastPurchasePrice |> Option.bind marginPct with
                                | None -> ()
                                | Some margin ->
                                    Html.div [
                                        prop.key "last-purchase-price-margin"
                                        prop.className "ml-auto"
                                        prop.text $"%s{translations.Product.Margin}%s{translations.Home.Colon} %.0f{margin}%%"
                                    ]
                            ]
                        ]

                        ActionsDropdown "last-purchase-price" "mb-0" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoneyOptional lastPurchasePrice) []

                        // -- Average Purchase Price (form hint) ----
                        Daisy.fieldsetLabel [
                            prop.key "average-purchase-price-hint"
                            prop.className "grid grid-cols-[1fr_auto] items-center mb-4 italic"
                            prop.children [
                                Html.span [
                                    prop.key "average-label"
                                    prop.children [
                                        Html.text $"%s{translations.Product.AveragePriceOver1Y}%s{translations.Home.Colon} "
                                        Html.text (
                                            match purchasePrices.AverageOver1Y with
                                            | Some price -> price.ValueWithCurrencySymbol
                                            | None -> "-"
                                        )
                                    ]
                                ]
                                Html.span [
                                    prop.key "average-margin"
                                    prop.className "text-right"
                                    prop.text (
                                        match purchasePrices.AverageOver1Y |> Option.bind marginPct with
                                        | Some margin -> $"%.0f{margin}%%"
                                        | None -> "-"
                                    )
                                ]
                            ]
                        ]
                    | _ -> ()

                // -- Stock ----
                match model.Stock, translations with
                | Remote.Empty, _ -> ()
                | Remote.Loading, _
                | _, TranslationsMissing PageCode.Product -> Daisy.skeleton [ prop.className "h-24 w-full"; prop.key "stock-skeleton" ]
                | Remote.LoadError apiError, _ -> Alert.apiError "stock-load-error" apiError fullContext.User
                | Remote.Loaded stock, _ ->
                    Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text translations.Product.Stock ]

                    ActionsDropdown "stock" "mb-4" (fullContext.User.AccessTo Feat.Warehouse) (Value.Natural stock.Quantity) [
                        ActionProps.withIcon
                            "inventory-adjustment"
                            (icon fa6Solid.pencil)
                            translations.Product.StockAction.AdjustStockAfterInventory
                            (fun () -> drawerControl.Open(Drawer.AdjustStockAfterInventory stock))
                    ]
            ]
        ]
    ]