module Shopfoo.Client.Pages.Product.Actions

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Actions
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Remoting
open Shopfoo.Shared.Translations

type private Model = { Prices: Remote<Prices>; SaveStatus: Remote<unit> }

type private Msg = PricesFetched of ApiResult<GetPricesResponse>

[<RequireQualifiedAccess>]
module private Cmd =
    let loadPrices (cmder: Cmder, request) =
        cmder.ofApiRequest {
            Call = fun api -> api.Product.GetPrices request
            Error = Error >> PricesFetched
            Success = Ok >> PricesFetched
        }

let private init (fullContext: FullContext) sku =
    { Prices = Remote.Loading; SaveStatus = Remote.Empty }, // ↩
    Cmd.loadPrices (fullContext.PrepareRequest sku)

let private update (msg: Msg) (model: Model) =
    match msg with
    | PricesFetched(Ok response) -> { model with Prices = response.Prices |> Remote.ofOption }, Cmd.none
    | PricesFetched(Error apiError) -> { model with Prices = Remote.LoadError apiError }, Cmd.none

[<ReactComponent>]
let ActionsForm key fullContext sku (drawerControl: DrawerControl) =
    let model, _ = React.useElmish (init fullContext sku, update, [||])
    let translations = fullContext.Translations

    // As the drawers are opened from dropdown menus that are positioned above the side drawer,
    // we apply two fixing strategies:
    // 1. Blur the menu to hide it (on mouse out only?).
    // 2. Set a high z-index to the side drawer (see z-[9999] below).
    drawerControl.OnOpen(fun _ -> JS.blurActiveElement ())

    React.fragment [
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

                            Action.Emoji
                                "increase-list-price"
                                ("↗️", translations.Product.PriceAction.Increase)
                                (fun () -> drawerControl.Open(Drawer.ModifyPrice(priceModelTo Increase, prices)))

                            Action.Emoji
                                "decrease-list-price"
                                ("↘️", translations.Product.PriceAction.Decrease)
                                (fun () -> drawerControl.Open(Drawer.ModifyPrice(priceModelTo Decrease, prices)))

                            Action.Emoji
                                "remove-list-price"
                                ("🧹", translations.Product.PriceAction.Remove)
                                (fun () -> drawerControl.Open(Drawer.RemoveListPrice)) // TODO: [RemoveListPrice] use a confirmation modal instead

                        | None ->
                            Action.Emoji
                                "define-list-price"
                                ("✍️", translations.Product.PriceAction.Define)
                                (fun () -> drawerControl.Open(Drawer.DefineListPrice))
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

                        Action.Emoji
                            "increase"
                            ("↗️", translations.Product.PriceAction.Increase)
                            (fun () -> drawerControl.Open(ModifyPrice(priceModelTo Increase, prices)))

                        Action.Emoji
                            "decrease"
                            ("↘️", translations.Product.PriceAction.Decrease)
                            (fun () -> drawerControl.Open(ModifyPrice(priceModelTo Decrease, prices)))

                        Action.Emoji
                            "mark-as-sold-out"
                            ("🚫", translations.Product.PriceAction.MarkAsSoldOut)
                            (fun () -> drawerControl.Open MarkAsSoldOut) // TODO: [MarkAsSoldOut] use a confirmation modal instead
                    ]

                    // -- Stock ----
                    Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text translations.Product.Stock ]
                    ActionsDropdown "stock" (fullContext.User.AccessTo Feat.Warehouse) (Value.Natural 17) [ // TODO: Fetch stock
                        Action.Emoji
                            "inventory-adjustment"
                            ("✏️", translations.Product.StockAction.AdjustStockAfterInventory)
                            (fun () -> drawerControl.Open AdjustStockAfterInventory)
                    ]
                ]
            ]
    ]