[<RequireQualifiedAccess>]
module Shopfoo.Product.Model.Product

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Model.Extensions

let private validateBook (product: Product) =
    match product.Category with
    | Category.Bazaar _ -> Validation.Ok()
    | Category.Books book -> Product.Guard.BookSubtitle.Validate(book.Subtitle)

let validate (product: Product) =
    validation {
        let! _ = Product.Guard.SKU.Validate(product.SKU.Value)
        and! _ = Product.Guard.Name.Validate(product.Title)
        and! _ = Product.Guard.Description.Validate(product.Description)
        and! _ = Product.Guard.ImageUrl.Validate(product.ImageUrl.Url)
        return ()
    }