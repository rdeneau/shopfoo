module Shopfoo.Domain.Types.Catalog

open Shopfoo.Domain.Types.Errors

type Provider =
    | FakeStore
    | OpenLibrary

type OLID = OLID of string

type BookAuthor = { OLID: OLID; Name: string }

type Book = {
    ISBN: ISBN
    Subtitle: string
    Authors: BookAuthor list
    Tags: string list
}

[<RequireQualifiedAccess>]
type StoreCategory =
    | Clothing
    | Electronics
    | Jewelry

type StoreProduct = { FSID: FSID; Category: StoreCategory }

[<RequireQualifiedAccess>]
type Category =
    | Books of Book
    | Store of StoreProduct

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