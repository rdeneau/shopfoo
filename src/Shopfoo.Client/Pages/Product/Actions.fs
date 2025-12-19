module Shopfoo.Client.Pages.Product.Actions

open Elmish
open Feliz
open Feliz.DaisyUI
open Feliz.UseElmish
open Shopfoo.Client.Components
open Shopfoo.Client.Components.Actions
open Shopfoo.Client.Remoting
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Security
open Shopfoo.Shared.Remoting

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

let private update (fullContext: FullContext) (msg: Msg) (model: Model) =
    match msg with
    | PricesFetched(Ok response) -> { model with Prices = response.Prices |> Remote.ofOption }, Cmd.none
    | PricesFetched(Error apiError) -> { model with Prices = Remote.LoadError apiError }, Cmd.none

[<ReactComponent>]
    let translations = fullContext.Translations

    React.fragment [
        match model.Prices with
        | Remote.Empty -> ()
        | Remote.Loading -> Daisy.skeleton [ prop.className "h-64 w-full"; prop.key "prices-skeleton" ]
        | Remote.LoadError apiError -> Alert.apiError "prices-load-error" apiError fullContext.User
        | Remote.Loaded prices ->
            Daisy.fieldset [
                prop.key $"%s{key}-fieldset"
                prop.className "bg-base-200 border border-base-300 rounded-box p-4"
                prop.children [
                    Html.legend [
                        prop.key "product-actions-legend"
                        prop.className "text-sm"
                        prop.text $"⚡ %s{translations.Product.Actions}"
                    ]

                    Daisy.fieldsetLabel [ prop.key "retail-price-label"; prop.text translations.Product.RetailPrice ]
                    ActionsDropdown "retail-price" (fullContext.User.AccessTo Feat.Sales) (Value.OfMoney prices.RetailPrice) [
                        Action.Emoji("↗️", "increase", translations.Product.PriceAction.Increase, fun () -> ()) // TODO
                        Action.Emoji("↘️", "decrease", translations.Product.PriceAction.Decrease, fun () -> ()) // TODO
                        Action.Emoji("🚫", "unavailable", translations.Product.PriceAction.Unavailable, fun () -> ()) // TODO
                    ]

                    Daisy.fieldsetLabel [ prop.key "stock-label"; prop.text translations.Product.Stock ]
                    ActionsDropdown "stock" (fullContext.User.AccessTo Feat.Warehouse) (Value.Natural 17) [
                        Action.Emoji("✏️", "inventory-adjustment", translations.Product.StockAction.InventoryAdjustment, fun () -> ()) // TODO
                    ]
                ]
            ]
    ]