namespace Shopfoo.Server.Remoting

open Shopfoo.Catalog
open Shopfoo.Home

/// Singleton providing access to each of the Feat project APIs
type FeatApi(home: IHomeApi, product: IProductApi) =
    member val Home = home
    member val Product = product
