module Shopfoo.Product.Tests.Mocks.FakeStoreClientMock

open System.Threading.Tasks
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Data.FakeStore

type FakeStoreClientMock() =
    interface IFakeStoreClient with
        member _.GetProductsAsync() : Task<Result<ProductDto list, DataRelatedError>> = Task.FromResult(Ok [])