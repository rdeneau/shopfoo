[<RequireQualifiedAccess>]
module internal Shopfoo.Home.Data.Users

open Shopfoo.Domain.Types.Security

module private Claims =
    let guest: Claims =
        Map [
            Feat.About, Access.View // ↩
            Feat.Catalog, Access.View
        ]

    let catalogEditor: Claims =
        guest // ↩
        |> Map.add Feat.Catalog Access.Edit
        |> Map.add Feat.Sales Access.View
        |> Map.add Feat.Warehouse Access.View

    let sales: Claims =
        guest // ↩
        |> Map.add Feat.Sales Access.Edit
        |> Map.add Feat.Warehouse Access.Edit

    let productManager: Claims =
        guest // ↩
        |> Map.add Feat.Catalog Access.Edit
        |> Map.add Feat.Sales Access.Edit
        |> Map.add Feat.Warehouse Access.Edit

    let admin: Claims =
        productManager // ↩
        |> Map.add Feat.Admin Access.Edit

let personas = [
    User.LoggedIn("👤 Guest", Claims.guest)
    User.LoggedIn("✍️ Catalog editor", Claims.catalogEditor)
    User.LoggedIn("💰 Sales", Claims.sales)
    User.LoggedIn("🕵️ Product manager", Claims.productManager)
    User.LoggedIn("🛡️ Administrator", Claims.admin)
]