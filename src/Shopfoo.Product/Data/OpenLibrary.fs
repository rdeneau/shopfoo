module internal Shopfoo.Product.Data.OpenLibrary

open System
open System.Net.Http
open Shopfoo.Data.Http
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors

[<CLIMutable>]
type IsbnResponseDto = {
    /// Format: "/books/{olid}" ; Example: "/books/OL31838215M"
    Key: string
    /// Example: "Clean Architecture"
    Title: string
    /// Example: "A Craftsman's Guide to Software Structure and Design"
    Subtitle: string
}

type internal OpenLibraryClient(httpClient: HttpClient, serializerFactory: HttpApiSerializerFactory) =
    let serializer = serializerFactory.Json(HttpApiName.OpenLibrary)

    member _.GetBookByIsbnAsync(SKU isbn) =
        task {
            use request = HttpRequestMessage.Get(Uri $"/isbn/{isbn}")
            use! response = httpClient.SendAsync(request)
            let! content = response.TryReadContentAsStringAsync(request)
            return serializer.TryDeserializeResult<IsbnResponseDto>(content)
        }

[<RequireQualifiedAccess>]
module internal Mappers =
    module EntityToDomain =
        do ()