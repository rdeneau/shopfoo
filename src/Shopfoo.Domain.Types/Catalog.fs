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

type OLID =
    | OLID of string
    member this.Value = let (OLID v) = this in v

type BookAuthor = { OLID: OLID; Name: string }

type Book = {
    ISBN: ISBN
    Subtitle: string
    Authors: BookAuthor list
    Tags: string list
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