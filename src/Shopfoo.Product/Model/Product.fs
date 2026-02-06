[<RequireQualifiedAccess>]
module Shopfoo.Product.Model.Product

open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Model.Extensions

let private guard = Guard("Product")

let private validateBook (product: Product) =
    match product.Category with
    | Category.Bazaar _ -> Validation.Ok()
    | Category.Books book -> guard.Validate(Product.Guard.BookSubtitle, book.Subtitle)

let validate (product: Product) =
    validation {
        let! _ = guard.Validate(Product.Guard.SKU, product.SKU.Value)
        and! _ = guard.Validate(Product.Guard.Name, product.Title)
        and! _ = guard.Validate(Product.Guard.Description, product.Description)
        and! _ = guard.Validate(Product.Guard.ImageUrl, product.ImageUrl.Url)
        and! _ = validateBook product
        return ()
    }