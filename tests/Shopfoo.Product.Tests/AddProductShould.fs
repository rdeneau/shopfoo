namespace Shopfoo.Product.Tests

open System
open FsCheck
open Shopfoo.Client.Tests.FsCheckArbs
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Product.Tests
open Shopfoo.Product.Tests.Fakes
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
    member this.``reject invalid product``(FieldIssues issues) =
        async {
            let invalidProduct, errors =
                ((createValidBookProduct (), []), issues)
                ||> Seq.fold (fun (product, errors) issue -> issue.UpdateProduct product, issue.ExpectedError :: errors)

            use fixture = new ApiTestFixture()
            let! result = fixture.Api.AddProduct invalidProduct

            let cleanResult =
                match result with
                | Error(Error.Validation actual) -> Error(Error.Validation(actual |> List.sort))
                | _ -> result

            (invalidProduct, cleanResult) =! (invalidProduct, Error(Error.Validation errors))
        }

    [<Test>]
    member this.``add initial prices with EUR currency when adding book product``() =
        async {
            let product = createValidBookProduct ()

            use fixture = new ApiTestFixture()
            let! addResult = fixture.Api.AddProduct product

            match addResult with
            | Ok() ->
                // Verify prices were added by attempting to fetch them
                let! priceResult = fixture.Api.GetPrices product.SKU

                priceResult
                |> function
                    | Some prices ->
                        if prices.SKU <> product.SKU then
                            raise (Exception("Prices SKU does not match product SKU"))

                        if prices.Currency <> Currency.EUR then
                            raise (Exception($"Expected EUR currency but got {prices.Currency}"))
                    | None -> raise (Exception("Expected prices to be created but got None"))
            | Error err -> raise (Exception($"AddProduct failed: {err}"))
        }