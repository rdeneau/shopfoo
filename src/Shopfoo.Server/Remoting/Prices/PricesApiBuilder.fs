namespace Shopfoo.Server.Remoting.Prices

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type PricesApiBuilder(api: FeatApi) =
    static let claim = Claims.single Feat.Sales

    member _.Build() : PricesApi = {
        GetPrices = GetPricesHandler(api) |> Security.authorizeHandler (claim Access.View)
        SavePrices = SavePricesHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        MarkAsSoldOut = MarkAsSoldOutHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        RemoveListPrice = RemoveListPriceHandler(api) |> Security.authorizeHandler (claim Access.Edit)
    }