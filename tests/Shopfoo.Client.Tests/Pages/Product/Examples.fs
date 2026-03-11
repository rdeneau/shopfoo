module Shopfoo.Client.Tests.Examples

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog

[<RequireQualifiedAccess>]
module Empty =
    let bazaarProduct (fsid: FSID) : Product =
        let bazaar: BazaarProduct = { FSID = fsid; Category = BazaarCategory.Jewelry }

        {
            SKU = fsid.AsSKU
            Title = ""
            Description = ""
            Category = Category.Bazaar bazaar
            ImageUrl = ImageUrl.None
        }

    let bookProduct (isbn: ISBN) : Product =
        let book: Book = {
            ISBN = isbn
            Subtitle = ""
            Authors = Set.empty
            Tags = Set.empty
        }

        {
            SKU = isbn.AsSKU
            Title = ""
            Description = ""
            Category = Category.Books book
            ImageUrl = ImageUrl.None
        }

[<RequireQualifiedAccess>]
module MensCottonJacket =
    let category = BazaarCategory.Clothing
    let fsid = FSID 3

    let product: Product = {
        SKU = fsid.AsSKU
        Title = "Mens Cotton Jacket"
        Description = "Great outerwear jackets for Spring/Autumn/Winter."
        Category = Category.Bazaar { FSID = fsid; Category = category }
        ImageUrl = ImageUrl.Valid "https://fakestoreapi.com/img/71li-ujtlUL._AC_UX679_.jpg"
    }

[<RequireQualifiedAccess>]
module TidyFirst =
    let author: BookAuthor = { OLID = OLID "OL235459A"; Name = "Kent Beck" }
    let isbn = ISBN "978-0-13-468599-1"
    let subtitle = "A Personal Exercise in Empirical Software Design"
    let tag1: BookTag = "Refactoring"
    let tag2: BookTag = "Software Design"

    let product: Product = {
        SKU = isbn.AsSKU
        Title = "Tidy First?"
        Description = "A guide to tidying code as a preliminary step to making changes."
        Category =
            Category.Books {
                ISBN = isbn
                Subtitle = subtitle
                Authors = Set [ author ]
                Tags = Set [ tag1; tag2 ]
            }
        ImageUrl = ImageUrl.Valid "https://covers.openlibrary.org/b/isbn/978-0-13-468599-1-L.jpg"
    }