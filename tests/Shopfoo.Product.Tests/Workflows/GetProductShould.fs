namespace Shopfoo.Product.Tests

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Tests
open Shopfoo.Product.Tests.Examples
open Swensen.Unquote
open TUnit.Core

type GetProductShould() =
    [<Test>]
    member _.``1-Cache: return Some book from the cache``() =
        async {
            let bookRaw = CleanCode.Dto.bookRaw
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
            let sku = FakeElectronicProduct.sku
            use fixture = new ApiTestFixture()
            fixture.ConfigureFakeStoreClient [ FakeElectronicProduct.FakeStore.productDto ]
            let! _ = fixture.Api.GetProducts FakeStore // Populate cache used by GetProduct

            let! result = fixture.Api.GetProduct sku
            let actualSku = result |> Option.map _.SKU
            actualSku =! Some sku
        }

    [<Test>]
    member _.``2-FakeStore: return None when no products match the given FSID in FakeStore``() =
        async {
            use fixture = new ApiTestFixture()
            fixture.ConfigureFakeStoreClient [ FakeElectronicProduct.FakeStore.productDto ]
            let! _ = fixture.Api.GetProducts FakeStore // Populate cache used by GetProduct

            let! result = fixture.Api.GetProduct (FSID 999).AsSKU
            result =! None
        }

    [<Test>]
    member _.``3-OpenLibrary: return Some Product when book and related author and work exist in OpenLibrary``() =
        async {
            use fixture = new ApiTestFixture()

            fixture.ConfigureOpenLibraryClient(
                authors = [ UncleBob.OpenLibrary.authorDto ],
                books = [ CleanCode.OpenLibrary.bookDto ],
                works = [ CleanCode.OpenLibrary.workDto ]
            )

            let sku = CleanCode.OpenLibrary.bookId.AsSKU
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