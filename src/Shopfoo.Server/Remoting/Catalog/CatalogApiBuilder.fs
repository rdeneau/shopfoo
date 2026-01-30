namespace Shopfoo.Server.Remoting.Catalog

open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Server.Remoting
open Shopfoo.Shared.Remoting

[<Sealed>]
type CatalogApiBuilder(api: FeatApi) =
    static let pages =
        Set [
            PageCode.Home
            PageCode.Login
            PageCode.Product
        ]

    static let claim = Claims.single Feat.Catalog

    member _.Build() : CatalogApi = {
        GetBooksData = GetBooksDataHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        GetProducts = GetProductsHandler(api, pages) |> Security.authorizeHandler (claim Access.View)
        GetProduct = GetProductHandler(api, pages) |> Security.authorizeHandler (claim Access.View)
        SaveProduct = SaveProductHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        AddProduct = AddProductHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        SearchAuthors = SearchAuthorsHandler(api) |> Security.authorizeHandler (claim Access.Edit)
        SearchBooks = SearchBooksHandler(api) |> Security.authorizeHandler (claim Access.Edit)
    }