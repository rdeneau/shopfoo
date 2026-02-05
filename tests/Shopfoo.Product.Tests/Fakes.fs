module Shopfoo.Product.Tests.Fakes

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog

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