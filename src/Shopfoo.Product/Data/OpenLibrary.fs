/// Open Library
/// https://openlibrary.org/dev
module internal Shopfoo.Product.Data.OpenLibrary

open System
open System.Net.Http
open FsToolkit.ErrorHandling
open Shopfoo.Data.Http
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors

[<AutoOpen>]
module internal Dto =
    [<CLIMutable>]
    type AuthorDto = {
        /// Example: "/authors/OL2653686A"
        Key: string

        /// Example: "Robert C. Martin"
        Name: string

        /// Example: [ 10721801 ]
        Photos: int list
    }

    [<CLIMutable>]
    type BookDto = {
        /// Format: "/books/{olid}" ; Example: "/books/OL31838215M"
        Key: string

        /// Example: "Clean Architecture"
        Title: string

        /// Example: "A Craftsman's Guide to Software Structure and Design"
        Subtitle: string

        /// (Optional)
        Description: string option

        /// Example: [ 15126500 ]
        Covers: int list

        /// Example: [ { "key": "/works/OL19809141W" } ]
        Works: {| Key: string |} list
    }

    [<RequireQualifiedAccess>]
    type CoverKey =
        | Id of int
        | ISBN of string
        | OLID of string

    [<RequireQualifiedAccess>]
    type CoverSize =
        | Small
        | Medium
        | Large

    [<CLIMutable>]
    type WorkDto = {
        /// Format: "/works/{olid}" ; Example: "/works/OL19809141W"
        Key: string

        /// Example: "Clean Architecture"
        Title: string

        /// Example: "A Craftsman's Guide to Software Structure and Design"
        Subtitle: string

        /// Example: [ { "key": "/works/OL19809141W" } ]
        Authors: {| Author: {| Key: string |} |} list
    }

type internal OpenLibraryClientSettings = { CoverBaseUrl: string }

type internal OpenLibraryClient(httpClient: HttpClient, settings, serializerFactory: HttpApiSerializerFactory) =
    let serializer = serializerFactory.Json(HttpApiName.OpenLibrary)

    member private _.GetByKeyAsync<'dto>(key) =
        task {
            use request = HttpRequestMessage.Get(Uri $"%s{key}")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<'dto>(content)
        }

    member this.GetAuthorAsync(authorKey) =
        this.GetByKeyAsync<AuthorDto>($"%s{authorKey}.json")

    member this.GetBookByIsbnAsync(ISBN isbn) =
        this.GetByKeyAsync<BookDto>($"/isbn/%s{isbn}")

    member this.GetWorkAsync(workKey) =
        this.GetByKeyAsync<WorkDto>($"/works/%s{workKey}")

    member _.GetCoverUrl(coverKey, coverSize) =
        let key =
            match coverKey with
            | CoverKey.Id id -> $"id/%d{id}"
            | CoverKey.ISBN isbn -> $"isbn/%s{isbn}"
            | CoverKey.OLID olid -> $"olid/%s{olid}"

        let size =
            match coverSize with
            | CoverSize.Small -> "S"
            | CoverSize.Medium -> "M"
            | CoverSize.Large -> "L"

        $"%s{settings.CoverBaseUrl}/%s{key}-%s{size}.jpg"

[<RequireQualifiedAccess>]
module private Mappers =
    module DtoToModel =
        let mapAuthorOlid (author: AuthorDto) : OLID =
            match author.Key.TrimStart('/').Split('/') with
            | [| "authors"; olid |] -> OLID olid
            | _ -> failwithf $"Invalid author key format: %s{author.Key}"

        let mapAuthor (author: AuthorDto) : BookAuthor = { // ↩
            OLID = mapAuthorOlid author
            Name = author.Name
        }

        let mapBookCategory (authors: AuthorDto list) (book: BookDto) isbn : Category =
            Category.Books {
                ISBN = isbn
                Subtitle = book.Subtitle
                Authors = authors |> List.map mapAuthor
                Tags = []
            }

        let mapBook sku category imageUrl (book: BookDto) : Product = {
            SKU = sku
            Title = book.Title
            Description = book.Description |> Option.defaultValue ""
            Category = category
            ImageUrl = imageUrl
        }

        let mapCoverKey (ISBN isbn) covers =
            covers |> List.tryHead |> Option.map CoverKey.Id |> Option.defaultValue (CoverKey.ISBN isbn)

module internal Pipeline =
    let getProduct (client: OpenLibraryClient) isbn =
        taskResult {
            let! bookDto = client.GetBookByIsbnAsync isbn

            let! workDto = client.GetWorkAsync bookDto.Works[0].Key
            let! authorDtos = workDto.Authors |> List.traverseTaskResultM (fun x -> client.GetAuthorAsync x.Author.Key)
            let category = Mappers.DtoToModel.mapBookCategory authorDtos bookDto isbn

            let coverKey = bookDto.Covers |> Mappers.DtoToModel.mapCoverKey isbn
            let imageUrl = client.GetCoverUrl(coverKey, CoverSize.Small) |> ImageUrl.Valid

            return bookDto |> Mappers.DtoToModel.mapBook isbn category imageUrl
        }
        |> Async.AwaitTask