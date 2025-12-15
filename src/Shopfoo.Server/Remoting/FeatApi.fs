namespace Shopfoo.Server.Remoting

open Shopfoo.Server.Feat

type FeatApi(catalog: Catalog.Api, home: Home.Api) =
    member val Catalog = catalog
    member val Home = home