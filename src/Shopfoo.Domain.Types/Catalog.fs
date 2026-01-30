module Shopfoo.Domain.Types.Catalog

open Shopfoo.Domain.Types.Errors

type Provider =
    /// Bazaar provider
    | FakeStore
    /// Book provider
    | OpenLibrary

[<RequireQualifiedAccess>]
type BazaarCategory =
    | Clothing
    | Electronics
    | Jewelry

type BazaarProduct = { FSID: FSID; Category: BazaarCategory }

type BookAuthor = { OLID: OLID; Name: string }
type BookAuthorSearchResults = { Authors: BookAuthor list; TotalCount: int }

type SearchedBook = {
    EditionKey: OLID
    Title: string
    Subtitle: string
    Authors: Set<BookAuthor>
}

type BookSearchResults = { Books: SearchedBook list; TotalCount: int }

type BookTag = string

type Book = {
    ISBN: ISBN
    Subtitle: string
    Authors: Set<BookAuthor>
    Tags: Set<BookTag>
}

[<RequireQualifiedAccess>]
type Category =
    | Bazaar of BazaarProduct
    | Books of Book

type ImageUrl = {
    Url: string
    Broken: bool
} with
    static member Valid(url) : ImageUrl = { Url = url; Broken = false }
    static member None: ImageUrl = { Url = ""; Broken = true }

type Product = {
    SKU: SKU
    Title: string
    Description: string
    Category: Category
    ImageUrl: ImageUrl
}

[<RequireQualifiedAccess>]
module Product =
    module Guard =
        let SKU = GuardCriteria.Create(required = true)
        let Name = GuardCriteria.Create(required = true, maxLength = 128)
        let Description = GuardCriteria.Create(maxLength = 512)
        let ImageUrl = GuardCriteria.None