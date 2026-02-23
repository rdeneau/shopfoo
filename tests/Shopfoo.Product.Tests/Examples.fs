module Shopfoo.Product.Tests.Examples

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary

[<RequireQualifiedAccess>]
module UncleBob =
    let olid = OLID "OL1234567A"
    let name = "Robert C. Martin"

    module OpenLibrary =
        let key = AuthorKey.Make olid.Value

        let authorDto = {
            Key = key.Path
            Name = name
            Photos = []
        }

[<RequireQualifiedAccess>]
module CleanCode =
    let isbn = ISBN "978-0-13-468599-1"
    let title = "Clean Code"
    let subtitle = "A Handbook of Agile Software Craftsmanship"
    let image = "https://example.com/clean-code.jpg"

    module Domain =
        let book: Book = {
            ISBN = isbn
            Subtitle = subtitle
            Authors = Set [ { OLID = UncleBob.olid; Name = UncleBob.name } ]
            Tags = Set []
        }

        let product: Product = {
            SKU = isbn.AsSKU
            Title = title
            Description = ""
            Category = Category.Books book
            ImageUrl = ImageUrl.Valid image
        }

    module Dto =
        let bookRaw: BookRaw = {
            ISBN = isbn
            Title = title
            Subtitle = subtitle
            Description = ""
            Authors = [ { Id = UncleBob.olid; Name = UncleBob.name } ]
            Image = image
            Tags = []
        }

    module OpenLibrary =
        let bookId = OLID "OL31838215M"
        let bookKey = BookKey.FromOlid bookId
        let workKey = WorkKey.Make "OL19809141W"

        let bookDto = {
            Key = bookKey.Path
            Title = title
            Subtitle = subtitle
            Description = None
            Covers = [ 8936088 ]
            Works = [ {| Key = workKey.Path |} ]
            Isbn10 = None
            Isbn13 = Some [ isbn.Value ]
        }

        let workDto = {
            Key = workKey.Path
            Title = title
            Subtitle = subtitle
            Authors = [ {| Author = {| Key = UncleBob.OpenLibrary.authorDto.Key |} |} ]
        }

[<RequireQualifiedAccess>]
module FakeElectronicProduct =
    let productId: ProductId = 23
    let fsid = FSID productId
    let sku = fsid.AsSKU
    let title = "Test Electronic Product"
    let description = "A test electronic product from FakeStore"
    let image = "https://example.com/test-electronic-product.jpg"

    module Domain =
        let product: Product = {
            SKU = sku
            Title = title
            Description = description
            Category = Category.Bazaar { FSID = fsid; Category = BazaarCategory.Electronics }
            ImageUrl = ImageUrl.Valid image
        }

    module FakeStore =
        let productDto: ProductDto = {
            Id = productId
            Title = title
            Price = 19.99m
            Description = description
            Category = "electronics"
            Image = image
        }