namespace Shopfoo.Product.Tests

open System
open FsCheck
open Shopfoo.Client.Tests.FsCheckArbs
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data.Books
open Shopfoo.Product.Tests
open Shopfoo.Product.Tests.Examples
open Shopfoo.Product.Tests.Types
open Swensen.Unquote
open TUnit.Core

[<RequireQualifiedAccess>]
type FieldIssueType =
    | ISBNEmpty of NullOrWhitespace
    | NameEmpty of NullOrWhitespace
    | NameTooLong of TooLong
    | DescriptionTooLong of TooLong
    | BookSubtitleTooLong of TooLong

type FieldIssue = {
    Name: string
    Value: string
    Criteria: string
    UpdateProduct: Product -> Product
} with
    member prop.ExpectedError: GuardClauseError = {
        EntityName = "Product"
        ErrorMessage = $"%s{prop.Name} '%s{prop.Value}' does not satisfy the criteria: %s{prop.Criteria}, trailing whitespaces excluded"
    }

[<AutoOpen>]
module Extensions =
    let private getBook product =
        match product.Category with
        | Category.Bazaar _ -> failwith "Expected book product"
        | Category.Books book -> book

    let replaceTrailingWhitespaceWith nonWhitespaceChar (chars: char array) =
        let lastIndex = chars.Length - 1

        if chars[lastIndex] |> Char.IsWhiteSpace then
            if nonWhitespaceChar |> Char.IsWhiteSpace then
                failwith "nonWhitespaceChar should not be a whitespace character"

            chars[lastIndex] <- nonWhitespaceChar

    type private NullOrWhitespace with
        member input.ToFieldIssue(name, updateProductByValue) : FieldIssue =
            let value =
                match input with
                | NullOrWhitespace.Null -> null
                | NullOrWhitespace.Empty -> ""
                | NullOrWhitespace.Whitespaces(NonEmptyArray chars) -> chars |> Array.map _.Char |> String.Concat

            {
                Name = name
                Value = value
                Criteria = "required"
                UpdateProduct = updateProductByValue value
            }

    type private TooLong with
        member input.ToFieldIssue(name, MaxLength maxLength, updateProduct) : FieldIssue =
            let (TooLong(NonEmptyArray exceedingChars)) = input
            exceedingChars |> replaceTrailingWhitespaceWith '·'

            let prefix = $"(%i{maxLength}+%i{exceedingChars.Length})"
            let fillerLength = max 1 (maxLength - prefix.Length)
            let filler = String('-', fillerLength)
            let value = prefix + filler + (String.Concat exceedingChars)

            {
                Name = name
                Value = value
                Criteria = $"%i{maxLength} character long max"
                UpdateProduct = updateProduct value
            }

    [<RequireQualifiedAccess>]
    module private FieldIssue =
        let ofType issueType : FieldIssue =
            match issueType with
            | FieldIssueType.ISBNEmpty input -> input.ToFieldIssue("SKU", fun value product -> { product with SKU = (ISBN value).AsSKU })
            | FieldIssueType.NameEmpty input -> input.ToFieldIssue("Name", fun value product -> { product with Title = value })
            | FieldIssueType.NameTooLong input -> input.ToFieldIssue("Name", MaxLength 128, fun value product -> { product with Title = value })

            | FieldIssueType.DescriptionTooLong input ->
                input.ToFieldIssue("Description", MaxLength 512, fun value product -> { product with Description = value })

            | FieldIssueType.BookSubtitleTooLong input ->
                input.ToFieldIssue(
                    "BookSubtitle",
                    MaxLength 256,
                    fun value product ->
                        let updatedBook = { getBook product with Subtitle = value }
                        { product with Category = Category.Books updatedBook }
                )

    let (|FieldIssues|) (NonEmptySet issueTypes: NonEmptySet<FieldIssueType>) = issueTypes |> Seq.map FieldIssue.ofType |> Seq.distinctBy _.Name // Avoid duplicate issues for the same field

type AddProductShould() =
    [<Test; ShopfooFsCheckProperty>]
    member _.``reject invalid product``(FieldIssues issues) =
        async {
            let invalidProduct, errors =
                ((CleanCode.Domain.product, []), issues)
                ||> Seq.fold (fun (product, errors) issue -> issue.UpdateProduct product, issue.ExpectedError :: errors)

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.AddProduct(invalidProduct, Currency.EUR)

            let cleanResult =
                match result with
                | Error(Error.Validation actual) -> Error(Error.Validation(actual |> List.sort))
                | _ -> result

            (invalidProduct, cleanResult) =! (invalidProduct, Error(Error.Validation errors))
        }

    [<Test>]
    [<Arguments(CurrencyEnum.EUR)>]
    [<Arguments(CurrencyEnum.USD)>]
    member _.``add initial prices too, in the given currency``(Currency.FromEnum currency) =
        async {
            use fixture = new ApiTestFixture()
            let product = CleanCode.Domain.product
            let! existingProduct = fixture.Api.GetProduct product.SKU
            existingProduct =! None // Assume that the product doesn't already exist

            let! addResult = fixture.Api.AddProduct(product, currency)

            let! actual =
                match addResult with
                | Error err -> async { return Error err }
                | Ok() ->
                    async {
                        let! addedProduct = fixture.Api.GetProduct product.SKU
                        let! addedPrice = fixture.Api.GetPrices product.SKU

                        match addedProduct, addedPrice with
                        | None, _ -> return Error(DataError(DataNotFound(product.SKU.Value, "Product")))
                        | _, None -> return Error(DataError(DataNotFound(product.SKU.Value, "Prices")))
                        | Some product, Some prices -> return Ok(product, prices)
                    }

            let expectedPrices = {
                SKU = product.SKU
                Currency = currency
                ListPrice = None
                RetailPrice = RetailPrice.SoldOut
            }

            actual =! Ok(product, expectedPrices)
        }