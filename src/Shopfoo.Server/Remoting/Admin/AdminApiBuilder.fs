namespace Shopfoo.Server.Remoting.Admin

open Shopfoo.Domain.Types.Security
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type AdminApiBuilder(api: FeatApi) =
    static let claim = Claims.single Feat.Admin

    member _.Build() : AdminApi = { // ↩
        ResetProductCache = ResetProductCacheHandler(api) |> Security.authorizeHandler (claim Access.Edit)
    }