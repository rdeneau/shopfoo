namespace Shopfoo.Server.Remoting

open Shopfoo.Catalog
open Shopfoo.Server.Feat

type FeatApi(catalog: ICatalogApi, home: Home.Api) =
    member val Catalog = catalog
    member val Home = home