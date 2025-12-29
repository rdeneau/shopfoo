namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type PricesApiBuilder(api: FeatApi) =
    static let claim = Claims.single Feat.Sales
    static let stockClaims = Map [ Feat.Sales, Access.View; Feat.Warehouse, Access.View ]

    member _.Build() : PricesApi = {
        DetermineStock = DetermineStockHandler(api) |> Security.authorizeHandler stockClaims
        GetPrices = GetPricesHandler(api) |> Security.authorizeHandler (claim Access.View)
        SavePrices = SavePricesHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        MarkAsSoldOut = MarkAsSoldOutHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        RemoveListPrice = RemoveListPriceHandler(api) |> Security.authorizeHandler (claim Access.Edit)
    }