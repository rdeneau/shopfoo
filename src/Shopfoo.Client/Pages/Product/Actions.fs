module Shopfoo.Client.Pages.Product.Actions

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
open Shopfoo.Client.Components.Icon
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type private Dialog =
    | MarkAsSoldOut
    | RemoveListPrice

type private Model = {
    Prices: Remote<Prices>
    RemoveListPriceStatus: Remote<DateTime>
    SaveStatus: Remote<unit>
}

type private Msg =
    // TODO: | MarkAsSoldOut of ApiCall<Tbd>
    | PricesFetched of ApiResult<GetPricesResponse>
    | RemoveListPrice of SKU * ApiCall<unit>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadPrices (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.GetPrices request
            Error = Error >> PricesFetched
            Success = Ok >> PricesFetched
        }

    let removeListPrice (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.RemoveListPrice request
            Error = (fun apiError -> RemoveListPrice(request.Body, Done(Error apiError)))
            Success = (fun data -> RemoveListPrice(request.Body, Done(Ok data)))
        }

let private init (fullContext: FullContext) sku =
    {
        Prices = Remote.Loading
        RemoveListPriceStatus = Remote.Empty
        SaveStatus = Remote.Empty
    },
    Cmd.loadPrices (fullContext.PrepareRequest sku)

let private update (fullContext: FullContext) onSavePrice (msg: Msg) (model: Model) =
    match msg with
    | PricesFetched(Ok response) -> { model with Prices = response.Prices |> Remote.ofOption }, Cmd.none
    | PricesFetched(Error apiError) -> { model with Prices = Remote.LoadError apiError }, Cmd.none

    | RemoveListPrice(sku, Start) ->
        { model with RemoveListPriceStatus = Remote.Loading }, // ↩
        Cmd.removeListPrice (fullContext.PrepareRequest sku)

    | RemoveListPrice(_, Done result) ->
        match model.Prices with
        | Remote.Loaded prices ->
            {
                model with
                    Prices = Remote.Loaded { prices with ListPrice = None }
                    RemoveListPriceStatus = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult
            },
            Cmd.ofEffect (fun _ -> onSavePrice (prices, result |> Result.tryGetError))
        | _ ->
            // Ignore the msg that should not happen if prices are not loaded.
            model, Cmd.none

[<ReactComponent>]
let ActionsForm key fullContext sku (drawerControl: DrawerControl) onSavePrice =
    let model, dispatch =
        React.useElmish (init fullContext sku, update fullContext onSavePrice, [||])

    let translations = fullContext.Translations

    // As the drawers are opened from dropdown menus that are positioned above the side drawer,
    // we apply two fixing strategies:
    // 1. Blur the menu to hide it (on mouse out only?).
    // 2. Set a high z-index to the side drawer (see z-[9999] below).
    drawerControl.OnOpen(fun _ -> JS.blurActiveElement ())

    // When the drawer is closed after a price modification, we refresh the prices.
    drawerControl.OnClose(fun drawer ->
        match drawer with
        | ModifyPrice(_, savedPrices) -> dispatch (PricesFetched(Ok { Prices = Some savedPrices }))
        | _ -> ()
    )

    let modalRef = React.useElementRef ()
    let dialog, setDialog = React.useState Dialog.RemoveListPrice

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
        match dialog with
        | Dialog.MarkAsSoldOut -> () // TODO
        | Dialog.RemoveListPrice ->
            match model.RemoveListPriceStatus with
            | Remote.Loaded _ ->
                JS.runAfter // ↩
                    (TimeSpan.FromMilliseconds(500))
                    (fun () -> updateModal _.close())
            | _ -> ()
    )

    let openModal dialog =
        setDialog dialog
        updateModal _.showModal()

    let closeModal (e: MouseEvent) =
        e.preventDefault ()
        updateModal _.close()

    let dialogTranslations, onClick =
        match dialog with
        | Dialog.MarkAsSoldOut ->
            translations.Product.PriceAction.MarkAsSoldOutDialog, // ↩
            (fun _ -> ()) // TODO: dispatch msg
        | Dialog.RemoveListPrice ->
            translations.Product.PriceAction.RemoveListPriceDialog, // ↩
            (fun _ -> dispatch (RemoveListPrice(sku, Start)))

    React.fragment [
        Daisy.modal.dialog [
            prop.key $"%s{key}-dialog"
            prop.ref modalRef
            prop.children [
                Daisy.modalBox.div [
                    prop.key $"%s{key}-dialog-box"
                    prop.children [
                        Html.form [
                            prop.key $"%s{key}-dialog-form"
                            prop.children (
                                Daisy.button.button [
                                    button.sm
                                    button.circle
                                    button.ghost
                                    prop.className "absolute right-2 top-2"
                                    prop.text "✕"
                                    prop.onClick closeModal
                                ]
                            )
                        ]
                        Html.h3 [
                            prop.key $"%s{key}-dialog-title"
                            prop.className "font-bold text-lg"
                            prop.text translations.Home.Confirmation
                        ]
                        Html.p [
                            prop.key $"%s{key}-dialog-message"
                            prop.className "py-4"
                            prop.text dialogTranslations.Question
                        ]
                        Daisy.modalAction [
                            prop.key $"%s{key}-dialog-actions"
                            prop.children [
                                Daisy.button.button [
                                    button.secondary
                                    button.outline
                                    prop.key $"%s{key}-dialog-cancel-button"
                                    prop.text translations.Home.Cancel
                                    prop.onClick closeModal
                                ]
                                Buttons.SaveButton(
                                    key = $"%s{key}-dialog-confirm-price",
                                    label = dialogTranslations.Confirm,
                                    tooltipOk = translations.Home.Completed,
                                    tooltipError = (fun err -> translations.Home.Error(err.ErrorMessage)),
                                    tooltipProps = [ tooltip.left ],
                                    saveDate = model.RemoveListPriceStatus,
                                    disabled = false,
                                    onClick = onClick
                                )
                            ]
                        ]
                    ]
                ]
                Daisy.modalBackdrop [ prop.key $"%s{key}-dialog-backdrop"; prop.onClick closeModal ]
            ]
        ]

        match model.Prices, translations with
        | Remote.Empty, _ -> ()
        | Remote.Loading, _
        | _, TranslationsMissing PageCode.Product -> Daisy.skeleton [ prop.className "h-64 w-full"; prop.key "prices-skeleton" ]
        | Remote.LoadError apiError, _ -> Alert.apiError "prices-load-error" apiError fullContext.User
        | Remote.Loaded prices, _ ->
            Daisy.fieldset [
                prop.key $"%s{key}-fieldset"
                prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                prop.children [
                    Html.legend [
                        prop.key "product-actions-legend"
                        prop.className "text-sm"
                        prop.text $"⚡ %s{translations.Product.Actions}"
                    ]

                    // -- ListPrice ----
                    Daisy.fieldsetLabel [ prop.key "list-price-label"; prop.text translations.Product.ListPrice ]
                    ActionsDropdown "list-price" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoneyOptional prices.ListPrice) [
                        match prices.ListPrice with
                        | Some listPrice ->
                            let priceModelTo intent : PriceModel = {
                                Type = ListPrice
                                Value = listPrice
                                Intent = intent
                            }

                            Action.withIcon
                                "increase-list-price"
                                (icon fa6Solid.arrowUpWideShort)
                                translations.Product.PriceAction.Increase
                                (fun () -> drawerControl.Open(Drawer.ModifyPrice(priceModelTo Increase, prices)))

                            Action.withIcon
                                "decrease-list-price"
                                (icon fa6Solid.arrowDownWideShort)
                                translations.Product.PriceAction.Decrease
                                (fun () -> drawerControl.Open(Drawer.ModifyPrice(priceModelTo Decrease, prices)))

                            Action.withIcon
                                "remove-list-price"
                                (icon fa6Solid.eraser)
                                translations.Product.PriceAction.Remove
                                (fun () -> openModal Dialog.RemoveListPrice)

                        | None ->
                            Action.withIcon
                                "define-list-price"
                                (icon fa6Solid.circlePlus)
                                (translations.Product.PriceAction.Define + " 🚧")
                                (fun () -> drawerControl.Open(Drawer.DefineListPrice)) // TODO: DefineListPrice
                    ]

                    // -- RetailPrice ----
                    Daisy.fieldsetLabel [
                        prop.key "retail-price-label"
                        prop.children [
                            Html.text translations.Product.RetailPrice
                            match prices.ListPrice with
                            | Some listPrice when listPrice > prices.RetailPrice ->
                                match Money.tryCompute listPrice prices.RetailPrice (fun x y -> round (-100m * (x - y) / x)) with
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
                    ActionsDropdown "retail-price" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoney prices.RetailPrice) [
                        let priceModelTo intent : PriceModel = {
                            Type = RetailPrice
                            Value = prices.RetailPrice
                            Intent = intent
                        }

                        Action.withIcon
                            "increase-retail-price"
                            (icon fa6Solid.arrowUpWideShort)
                            translations.Product.PriceAction.Increase
                            (fun () -> drawerControl.Open(ModifyPrice(priceModelTo Increase, prices)))

                        Action.withIcon
                            "decrease-retail-price"
                            (icon fa6Solid.arrowDownWideShort)
                            translations.Product.PriceAction.Decrease
                            (fun () -> drawerControl.Open(ModifyPrice(priceModelTo Decrease, prices)))

                        Action.withIcon
                            "mark-as-sold-out"
                            (icon fa6Solid.ban)
                            (translations.Product.PriceAction.MarkAsSoldOut + " 🚧")
                            (fun () -> openModal Dialog.MarkAsSoldOut) // TODO: MarkAsSoldOut
                    ]

                    // -- Stock ----
                    Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text translations.Product.Stock ]
                    ActionsDropdown "stock" (fullContext.User.AccessTo Feat.Warehouse) (Value.Natural 17) [ // TODO: Fetch stock
                        Action.withIcon
                            "inventory-adjustment"
                            (icon fa6Solid.pencil)
                            (translations.Product.StockAction.AdjustStockAfterInventory + " 🚧")
                            (fun () -> drawerControl.Open AdjustStockAfterInventory) // TODO: AdjustStockAfterInventory
                    ]
                ]
            ]
    ]