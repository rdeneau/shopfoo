namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type PricesApiBuilder(api: FeatApi) =
    static let salesClaim = Claims.single Feat.Sales
    static let stockClaims = Map [ Feat.Sales, Access.View; Feat.Warehouse, Access.View ]
    static let warehouseClaim = Claims.single Feat.Warehouse

    member _.Build() : PricesApi = {
        AdjustStock = AdjustStockHandler(api) |> Security.authorizeHandler stockClaims
        DetermineStock = DetermineStockHandler(api) |> Security.authorizeHandler stockClaims
        GetPrices = GetPricesHandler(api) |> Security.authorizeHandler (salesClaim Access.View)
        GetPurchasePrices = GetPurchasePricesHandler(api) |> Security.authorizeHandler stockClaims
        GetSalesStats = GetSalesStatsHandler(api) |> Security.authorizeHandler (salesClaim Access.View)
        InputSale = InputSaleHandler(api) |> Security.authorizeHandler (salesClaim Access.Edit)
        SavePrices = SavePricesHandler(api) |> Security.authorizeHandler (salesClaim Access.Edit)
        MarkAsSoldOut = MarkAsSoldOutHandler(api) |> Security.authorizeHandler (salesClaim Access.Edit)
        ReceiveSupply = ReceiveSupplyHandler(api) |> Security.authorizeHandler (warehouseClaim Access.Edit)
        RemoveListPrice = RemoveListPriceHandler(api) |> Security.authorizeHandler (salesClaim Access.Edit)
    }