namespace Shopfoo.Product.Tests

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Data.FakeStore
open Shopfoo.Product.Data.OpenLibrary.Dto
open Swensen.Unquote
open TUnit.Core

[<AutoOpen>]
module private Data =
    module FakeStore =
        let productDto = {
            Id = 1
            Title = "Test Product"
            Price = 19.99m
            Description = "A test product from FakeStore"
            Category = "electronics"
            Image = "https://example.com/test-product.jpg"
        }

    module OpenLibrary =
        let bookId = OLID "OL31838215M"
        let bookKey = BookKey.ofOlid bookId
        let workKey = WorkKey.ofOlid (OLID "OL19809141W")
        let authorKey = AuthorKey.ofOlid (OLID "OL2653686A")

        let bookDto = {
            Key = bookKey.Value
            Title = "Clean Architecture"
            Subtitle = "A Craftsman's Guide to Software Structure and Design"
            Description = Some "Book about clean architecture"
            Covers = [ 8936088 ]
            Works = [ {| Key = workKey.Value |} ]
            Isbn10 = Some [ "0134494164" ]
            Isbn13 = Some [ "9780134494166" ]
        }

        let workDto = {
            Key = workKey.Value
            Title = "Clean Architecture"
            Subtitle = "A Craftsman's Guide to Software Structure and Design"
            Authors = [ {| Author = {| Key = authorKey.Value |} |} ]
        }

        let authorDto = {
            Key = authorKey.Value
            Name = "Robert C. Martin"
            Photos = [ 10721801 ]
        }

type GetProductShould() =
    [<Test>]
    member _.``1-Cache: return Some book from the cache``() =
        async {
            let bookRaw = {
                ISBN = ISBN "978-0-13-468599-1"
                Title = "Clean Code"
                Subtitle = "A Handbook of Agile Software Craftsmanship"
                Description = "A Handbook of Agile Software Craftsmanship"
                Authors = [ { Id = OLID "OL1234567A"; Name = "Robert C. Martin" } ]
                Image = "https://example.com/clean-code.jpg"
                Tags = []
            }

            let sku = bookRaw.ISBN.AsSKU

            use fixture = new ApiTestFixture(books = [ bookRaw ])
            let! result = fixture.Api.GetProduct sku

            let actualSku = result |> Option.map _.SKU
            actualSku =! Some sku
        }

    [<Test>]
    member _.``1-Cache: return None when no books in the cache match the given ISBN``() =
        async {
            let isbn = ISBN "978-0-00-000000-0"

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.GetProduct isbn.AsSKU
            result =! None
        }

    [<Test>]
    member _.``2-FakeStore: return Some Product from FakeStore``() =
        async {
            let sku = (FSID FakeStore.productDto.Id).AsSKU
            use fixture = new ApiTestFixture()
            fixture.ConfigureFakeStoreClient [ FakeStore.productDto ]
            let! _ = fixture.Api.GetProducts FakeStore // Populate cache used by GetProduct

            let! result = fixture.Api.GetProduct sku
            let actualSku = result |> Option.map _.SKU
            actualSku =! Some sku
        }

    [<Test>]
    member _.``2-FakeStore: return None when no products match the given FSID in FakeStore``() =
        async {
            use fixture = new ApiTestFixture()
            fixture.ConfigureFakeStoreClient [ FakeStore.productDto ]
            let! _ = fixture.Api.GetProducts FakeStore // Populate cache used by GetProduct

            let! result = fixture.Api.GetProduct (FSID 999).AsSKU
            result =! None
        }

    [<Test>]
    member _.``3-OpenLibrary: return Some Product when book and related author and work exist in OpenLibrary``() =
        async {
            use fixture = new ApiTestFixture()
            fixture.ConfigureOpenLibraryClient(authors = [ OpenLibrary.authorDto ], books = [ OpenLibrary.bookDto ], works = [ OpenLibrary.workDto ])

            let sku = OpenLibrary.bookId.AsSKU
            let! result = fixture.Api.GetProduct sku
            let actualSku = result |> Option.map _.SKU
            actualSku =! Some sku
        }

    [<Test>]
    member _.``3-OpenLibrary: return None given book not found in OpenLibrary``() =
        async {
            let olid = OLID "OL99999999M"
            use fixture = new ApiTestFixture()
            fixture.ConfigureOpenLibraryClient()

            let! result = fixture.Api.GetProduct olid.AsSKU
            result =! None
        }