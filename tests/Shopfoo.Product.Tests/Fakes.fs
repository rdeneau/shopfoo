module Shopfoo.Product.Tests.Fakes

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog

let createValidElectronicProduct () : Product =
    let fsid = FSID 23

    {
        SKU = fsid.AsSKU
        Title = "Test Electronic Product"
        Description = "A test electronic product from FakeStore"
        Category = Category.Bazaar { FSID = fsid; Category = BazaarCategory.Electronics }
        ImageUrl = ImageUrl.Valid "https://example.com/test-electronic-product.jpg"
    }

let createValidBookProduct () : Product =
    let isbn = ISBN "978-0-13-468599-1"

    {
        SKU = isbn.AsSKU
        Title = "Clean Code"
        Description = "A Handbook of Agile Software Craftsmanship"
        Category =
            Category.Books {
                ISBN = isbn
                Subtitle = "A Handbook of Agile Software Craftsmanship"
                Authors = Set [ { OLID = OLID "OL1234567A"; Name = "Robert C. Martin" } ]
                Tags = Set []
            }
        ImageUrl = ImageUrl.Valid "https://example.com/clean-code.jpg"
    }