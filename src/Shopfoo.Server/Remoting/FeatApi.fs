namespace Shopfoo.Server.Remoting

open Shopfoo.Catalog
open Shopfoo.Home

type FeatApi(catalog: ICatalogApi, home: IHomeApi) =
    member val Catalog = catalog
    member val Home = home