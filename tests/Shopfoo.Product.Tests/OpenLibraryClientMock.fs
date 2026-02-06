module Shopfoo.Product.Tests.Mocks.OpenLibraryClientMock

open System
open System.Threading.Tasks
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Data.OpenLibrary

type OpenLibraryClientMock() =
    interface IOpenLibraryClient with
        member _.GetAuthorAsync(authorKey: string) : Task<Result<AuthorDto, DataRelatedError>> =
            Task.FromResult(Error(DataException(NotImplementedException("Mock not implemented"))))

        member _.GetBookByIsbnAsync(ISBN isbn) : Task<Result<BookDto, DataRelatedError>> =
            Task.FromResult(Error(DataException(NotImplementedException("Mock not implemented"))))

        member _.GetBookByOlidAsync(OLID olid) : Task<Result<BookDto, DataRelatedError>> =
            Task.FromResult(Error(DataException(NotImplementedException("Mock not implemented"))))

        member _.GetWorkAsync(workKey: string) : Task<Result<WorkDto, DataRelatedError>> =
            Task.FromResult(Error(DataException(NotImplementedException("Mock not implemented"))))

        member _.SearchAuthorsAsync(searchTerm: string) : Task<Result<SearchAuthorsResponseDto, DataRelatedError>> =
            Task.FromResult(Error(DataException(NotImplementedException("Mock not implemented"))))

        member _.SearchBooksAsync(searchTerm: string) : Task<Result<SearchBooksResponseDto, DataRelatedError>> =
            Task.FromResult(Error(DataException(NotImplementedException("Mock not implemented"))))

        member _.GetCoverUrl(coverKey: CoverKey, coverSize: CoverSize) : string = "https://mock-cover.local/image.jpg"