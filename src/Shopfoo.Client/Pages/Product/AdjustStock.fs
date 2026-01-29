module Shopfoo.Client.Pages.Product.AdjustStock

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Remoting
open Shopfoo.Common
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Shared.Remoting

type private Msg = // ↩
    | StockChanged of Stock
    | AdjustStock of ApiCall<unit>

type private Model = {
    Stock: Stock
    SaveDate: Remote<DateTime>
} with
    /// Closes the drawer, passing the modified stock back to drawer opener, only if it has been saved.
    member model.CloseDrawer(drawerControl: DrawerControl) =
        let drawer =
            match model.SaveDate with
            | Remote.Loaded _ -> Some(Drawer.AdjustStockAfterInventory model.Stock)
            | _ -> None

        drawerControl.Close(?drawer = drawer)

[<RequireQualifiedAccess>]
module private Cmd =
    let adjustStock (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Prices.AdjustStock request
            Error = AdjustStock << Done << Error
            Success = AdjustStock << Done << Ok
        }

let private init stock = { Stock = stock; SaveDate = Remote.Empty }, Cmd.none

let private update (fullContext: FullContext) onSave (msg: Msg) (model: Model) =
    match msg with
    | StockChanged stock -> // ↩
        { model with Stock = stock }, Cmd.none

    | AdjustStock Start ->
        { model with SaveDate = Remote.Loading }, // ↩
        Cmd.adjustStock (fullContext.PrepareRequest model.Stock)

    | AdjustStock(Done result) ->
        { model with SaveDate = result |> Result.map (fun () -> DateTime.Now) |> Remote.ofResult },
        Cmd.ofEffect (fun _ -> onSave (model.Stock, result |> Result.tryGetError))

[<ReactComponent>]
let AdjustStockForm key (fullContext: FullContext) stock drawerControl onSave =
    let model, dispatch = React.useElmish (init stock, update fullContext onSave, [||])

    // Close the drawer after success with a short delay (500ms),
    // time to let the user get this result visually.
    React.useEffect (fun () ->
        match model.SaveDate with
        | Remote.Loaded _ ->
            JS.runAfter // ↩
                (TimeSpan.FromMilliseconds(500))
                (fun () -> model.CloseDrawer(drawerControl))
        | _ -> ()
    )

    let translations = fullContext.Translations

    let currentStock = stock.Quantity
    let newStock = model.Stock.Quantity
    let stockChange = newStock - currentStock

    let afterSaveOk =
        match model.SaveDate with
        | Remote.Loaded _ -> true
        | _ -> false

    let canSave = (stockChange <> 0)

    Daisy.fieldset [
        prop.key $"%s{key}-fieldset"
        prop.children [
            Html.legend [
                prop.key $"%s{key}-legend"
                prop.className "text-base font-bold mb-2 flex items-center"
                prop.text translations.Product.StockAction.AdjustStockAfterInventory
            ]

            // -- Current Stock (Before Inventory) ----
            Daisy.fieldsetLabel [ // ↩
                prop.key $"%s{key}-current-stock-fieldset-label"
                prop.text translations.Product.StockBeforeInventory
            ]

            Daisy.label.input [
                prop.key $"{key}-current-stock-label"
                prop.className "bg-base-300 w-full mb-2"
                prop.children [
                    Html.input [
                        prop.key $"%s{key}-current-stock-input"
                        prop.ariaReadOnly true
                        prop.readOnly true
                        prop.value currentStock
                    ]
                ]
            ]

            // -- New Stock (After Inventory) ----
            Daisy.fieldsetLabel [ prop.key $"%s{key}-new-stock-fieldset-label"; prop.text translations.Product.StockAfterInventory ]

            Daisy.label.input [
                prop.key $"{key}-new-stock-label"
                prop.className [
                    "w-full mb-4"
                    if afterSaveOk then
                        "bg-base-300"
                ]
                prop.children [
                    Html.input [
                        prop.key $"%s{key}-new-stock-input"
                        prop.type' "number"
                        prop.className "flex-1 validator"
                        prop.required true
                        prop.value newStock

                        if afterSaveOk then
                            prop.ariaReadOnly true
                            prop.readOnly true
                        else
                            prop.onChange (fun (value: int) -> dispatch (StockChanged { model.Stock with Quantity = value }))

                            if canSave then
                                prop.onKeyUp (Feliz.key.enter, fun _ -> dispatch (AdjustStock Start))

                        prop.min 0
                        prop.step 1
                    ]
                ]
            ]

            // -- Buttons ----
            Html.div [
                prop.key $"%s{key}-save-div"
                prop.className "flex justify-end"
                prop.children [
                    // -- Close ----
                    Daisy.button.button [
                        button.secondary
                        button.outline
                        prop.key $"%s{key}-close-button"
                        prop.className "mr-2"
                        prop.onClick (fun _ -> model.CloseDrawer(drawerControl))
                        prop.text translations.Home.Close
                    ]

                    // -- Save Stocks ----
                    Buttons.SaveButton(
                        key = "save-price",
                        label = translations.Home.Save,
                        tooltipOk = translations.Home.SavedOk translations.Product.Stock,
                        tooltipError = (fun err -> translations.Home.SavedError(translations.Product.Stock, err.ErrorMessage)),
                        tooltipProps = [ tooltip.left ],
                        saveDate = model.SaveDate,
                        disabled = (afterSaveOk || not canSave),
                        onClick = (fun () -> dispatch (AdjustStock Start))
                    )
                ]
            ]
        ]
    ]