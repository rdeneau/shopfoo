/// Open Library
/// https://openlibrary.org/dev
module internal Shopfoo.Product.Data.OpenLibrary

open System
open System.Net
open System.Net.Http
open System.Text.Json.Serialization
open System.Threading.Tasks
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

        /// Example: [ "0132350882" ]
        [<JsonPropertyName("isbn_10")>]
        Isbn10: string list option

        /// Example: [ "9780132350884" ]
        [<JsonPropertyName("isbn_13")>]
        Isbn13: string list option
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

    [<CLIMutable>]
    type AuthorSearchDto = {
        /// Example: "OL2653686A"
        Key: string

        /// Example: "Robert C. Martin"
        Name: string
    }

    [<CLIMutable>]
    type SearchAuthorsResponseDto = { NumFound: int; Docs: AuthorSearchDto list }

    [<CLIMutable>]
    type BookSearchDto = {
        /// Example: "/works/OL17618370W"
        Key: string

        /// Example: "Clean Code"
        Title: string

        /// Example: [ "OL2653686A" ]
        [<JsonPropertyName("author_key")>]
        AuthorKey: string list option

        /// Example: [ "Robert C. Martin" ]
        [<JsonPropertyName("author_name")>]
        AuthorName: string list option

        /// Example: { "docs": [ { "key": "/books/OL26222911M", "title": "Clean Code" } ] }
        Editions: {| Docs: {| Key: string; Title: string |} list |} option
    }

    [<CLIMutable>]
    type SearchBooksResponseDto = { NumFound: int; Docs: BookSearchDto list }

type internal OpenLibraryClientSettings = { CoverBaseUrl: string }

[<Interface>]
type IOpenLibraryClient =
    abstract member GetAuthorAsync: string -> Task<Result<AuthorDto, DataRelatedError>>
    abstract member GetBookByIsbnAsync: ISBN -> Task<Result<BookDto, DataRelatedError>>
    abstract member GetBookByOlidAsync: OLID -> Task<Result<BookDto, DataRelatedError>>
    abstract member GetWorkAsync: string -> Task<Result<WorkDto, DataRelatedError>>
    abstract member SearchAuthorsAsync: string -> Task<Result<SearchAuthorsResponseDto, DataRelatedError>>
    abstract member SearchBooksAsync: string -> Task<Result<SearchBooksResponseDto, DataRelatedError>>
    abstract member GetCoverUrl: CoverKey * CoverSize -> string

type internal OpenLibraryClient(httpClient: HttpClient, settings, serializerFactory: HttpApiSerializerFactory) =
    let serializer = serializerFactory.Json(HttpApiName.OpenLibrary)

    member private _.GetByKeyAsync<'dto>(key) =
        task {
            use request = HttpRequestMessage.Get(Uri.Relative $"%s{key}")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<'dto>(content)
        }

    member this.GetAuthorAsync(authorKey) = this.GetByKeyAsync<AuthorDto>($"%s{authorKey}.json")
    member this.GetBookByIsbnAsync(ISBN isbn) = this.GetByKeyAsync<BookDto>($"/isbn/%s{isbn}")
    member this.GetBookByOlidAsync(OLID olid) = this.GetByKeyAsync<BookDto>($"/books/%s{olid}.json")

    member this.GetWorkAsync(workKey: string) =
        let key =
            if workKey.StartsWith("/works/") then workKey
            elif workKey.StartsWith("works/") then $"/%s{workKey}"
            else $"/works/%s{workKey}"

        this.GetByKeyAsync<WorkDto>(key)

    member _.SearchAuthorsAsync(searchTerm: string) =
        task {
            let encodedTerm = WebUtility.UrlEncode(searchTerm)
            use request = HttpRequestMessage.Get(Uri.Relative $"/search/authors.json?q=%s{encodedTerm}&limit=10")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<SearchAuthorsResponseDto>(content)
        }

    member _.SearchBooksAsync(searchTerm: string) =
        task {
            let encodedTerm = WebUtility.UrlEncode(searchTerm)
            let fields = "author_key,author_name,key,title,editions"
            use request = HttpRequestMessage.Get(Uri.Relative $"/search.json?q=%s{encodedTerm}&limit=10&language=eng&fields=%s{fields}")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<SearchBooksResponseDto>(content)
        }

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

    interface IOpenLibraryClient with
        member this.GetAuthorAsync(authorKey) = this.GetAuthorAsync(authorKey)
        member this.GetBookByIsbnAsync(isbn) = this.GetBookByIsbnAsync(isbn)
        member this.GetBookByOlidAsync(olid) = this.GetBookByOlidAsync(olid)
        member this.GetWorkAsync(workKey) = this.GetWorkAsync(workKey)
        member this.SearchAuthorsAsync(searchTerm) = this.SearchAuthorsAsync(searchTerm)
        member this.SearchBooksAsync(searchTerm) = this.SearchBooksAsync(searchTerm)
        member this.GetCoverUrl(coverKey, coverSize) = this.GetCoverUrl(coverKey, coverSize)

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

        let mapSearchedAuthor (author: AuthorSearchDto) : BookAuthor = { // ↩
            OLID = OLID author.Key
            Name = author.Name
        }

        let mapSearchedAuthors (response: SearchAuthorsResponseDto) : BookAuthorSearchResults = { // ↩
            Authors = response.Docs |> List.map mapSearchedAuthor
            TotalCount = response.NumFound
        }

        let mapEditionKey (editionKey: string) : OLID =
            match editionKey.TrimStart('/').Split('/') with
            | [| "books"; olid |] -> OLID olid
            | _ -> failwithf $"Invalid edition key format: %s{editionKey}"

        let mapSearchedBook (bookDto: BookSearchDto) : SearchedBook option =
            match bookDto.AuthorKey, bookDto.AuthorName with
            | Some authorKeys, Some authorNames ->
                let authors =
                    List.zip (authorKeys |> List.truncate authorNames.Length) authorNames
                    |> List.map (fun (key, name) -> { OLID = OLID key; Name = name })
                    |> Set.ofList

                let editionKey =
                    bookDto.Editions
                    |> Option.bind (fun editions -> editions.Docs |> List.tryHead)
                    |> Option.map (fun edition -> mapEditionKey edition.Key)
                    |> Option.defaultWith (fun () -> failwithf $"No edition found for book: %s{bookDto.Title}")

                Some {
                    EditionKey = editionKey
                    Title = bookDto.Title
                    Subtitle = "" // Subtitle not available in search results
                    Authors = authors
                }
            | _ -> None // Skip books without author information

        let mapSearchedBooks (response: SearchBooksResponseDto) : BookSearchResults = {
            Books = response.Docs |> List.choose mapSearchedBook
            TotalCount = response.NumFound
        }

        let mapBookCategory (authors: AuthorDto list) (book: BookDto) isbn : Category =
            Category.Books {
                ISBN = isbn
                Subtitle = book.Subtitle
                Authors = authors |> Seq.map mapAuthor |> Set.ofSeq
                Tags = Set.empty
            }

        let mapBook sku category imageUrl (book: BookDto) : Product = {
            SKU = sku
            Title = book.Title
            Description = book.Description |> Option.defaultValue ""
            Category = category
            ImageUrl = imageUrl
        }

        let mapCoverKey (ISBN isbn) covers : CoverKey =
            covers // ↩
            |> List.tryHead
            |> Option.map CoverKey.Id
            |> Option.defaultValue (CoverKey.ISBN isbn)

module internal Pipeline =
    let getProductByOlid (client: IOpenLibraryClient) olid =
        taskResult {
            let! bookDto = client.GetBookByOlidAsync olid

            let isbn =
                match bookDto.Isbn13, bookDto.Isbn10 with
                | Some(isbn13 :: _), _ -> ISBN isbn13
                | _, Some(isbn10 :: _) -> ISBN isbn10
                | _ -> ISBN ""

            let! workDto = client.GetWorkAsync bookDto.Works[0].Key
            let! authorDtos = workDto.Authors |> List.traverseTaskResultM (fun x -> client.GetAuthorAsync x.Author.Key)

            let category = Mappers.DtoToModel.mapBookCategory authorDtos bookDto isbn
            let coverKey = Mappers.DtoToModel.mapCoverKey isbn bookDto.Covers
            let imageUrl = client.GetCoverUrl(coverKey, CoverSize.Medium) |> ImageUrl.Valid

            return Mappers.DtoToModel.mapBook olid.AsSKU category imageUrl bookDto
        }
        |> Async.AwaitTask

    let searchAuthors (client: IOpenLibraryClient) searchTerm =
        async {
            let! result = client.SearchAuthorsAsync(searchTerm) |> Async.AwaitTask
            return result |> liftDataRelatedError |> Result.map Mappers.DtoToModel.mapSearchedAuthors
        }

    let searchBooks (client: IOpenLibraryClient) searchTerm =
        async {
            let! result = client.SearchBooksAsync(searchTerm) |> Async.AwaitTask
            return result |> liftDataRelatedError |> Result.map Mappers.DtoToModel.mapSearchedBooks
        }