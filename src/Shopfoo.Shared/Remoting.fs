module Shopfoo.Shared.Remoting

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Security
open Shopfoo.Domain.Types.Translations
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Translations

[<RequireQualifiedAccess>]
type FullContext = {
    Lang: Lang
    User: User
    Token: AuthToken option
    Translations: AppTranslations
} with
    static member Default: FullContext = {
        Lang = Lang.English
        User = User.Anonymous
        Token = None
        Translations = AppTranslations()
    }

    member this.FillTranslations(translations: Translations) = // ↩
        { this with Translations = this.Translations.Fill translations }

    member this.ResetTranslations() = // ↩
        { this with Translations = AppTranslations() }

module Route =
    /// Defines how routes are generated on server and mapped from client.
    /// E.g. api/Search/GetFirstPage
    let builder pageName methodName = $"/api/%s{pageName}/%s{methodName}"

type Request<'t> = {
    Token: AuthToken option
    Lang: Lang
    Body: 't
}

type ServerError =
    | ApiError of ApiError
    | AuthError of AuthError

type Response<'a> = Result<'a, ServerError>

[<Interface>]
type IResponseBuilder<'a, 'b> =
    abstract ApiError: error: Error * ?key: TranslationKey -> Response<'a>
    abstract Ok: 'b -> Response<'a>

module ResponseBuilder =
    let private (|TranslationsOrEmpty|) =
        function
        | Ok translations -> translations
        | Error(_: Error) -> Translations.Empty

    let plain<'a> user =
        { new IResponseBuilder<'a, 'a> with
            member _.ApiError(error, ?key) =
                ApiError.FromError(error, User.errorDetailLevel user, ?key = key)
                |> ServerError.ApiError
                |> Response.Error

            member _.Ok response = // ↩
                Response.Ok response
        }

    let withTranslations<'a> user (TranslationsOrEmpty translations) =
        { new IResponseBuilder<'a * Translations, 'a> with
            member _.ApiError(error, ?key) =
                ApiError.FromError(error, User.errorDetailLevel user, ?key = key)
                |> ServerError.ApiError
                |> Response.Error

            member _.Ok response =
                Response.Ok response |> Result.map (fun response -> response, translations)
        }

type QueryDataAndTranslations<'query> = { // ↩
    Query: 'query
    TranslationPages: PageCode Set
}

type Command<'command'> = Request<'command'> -> Async<Response<unit>>
type Query<'query, 'response> = Request<'query> -> Async<Response<'response>>
type QueryWithTranslations<'query, 'response> = Query<QueryDataAndTranslations<'query>, 'response * Translations>

type IApi = interface end

[<AutoOpen>]
module CatalogApi =
    open Shopfoo.Domain.Types.Catalog

    type GetProductsResponse = { Products: Product list }
    type GetProductResponse = { Product: Product option }

    type CatalogApi = {
        GetProducts: QueryWithTranslations<unit, GetProductsResponse>
        GetProduct: QueryWithTranslations<SKU, GetProductResponse>
        SaveProduct: Command<Product>
    } with
        interface IApi

[<AutoOpen>]
module HomeApi =
    type HomeIndexResponse = { Personas: User list }

    type GetTranslationsRequest = { Lang: Lang; PageCodes: Set<PageCode> }
    type GetTranslationsResponse = { Lang: Lang; Translations: Translations }

    type HomeApi = {
        Index: QueryWithTranslations<unit, HomeIndexResponse>
        GetTranslations: Query<GetTranslationsRequest, GetTranslationsResponse>
    } with
        interface IApi

[<AutoOpen>]
module PricesApi =
    open Shopfoo.Domain.Types.Sales

    type GetPricesResponse = { Prices: Prices option }
    type PriceCommand = { SKU: SKU }

    type PricesApi = {
        DetermineStock: Query<SKU, Stock>
        GetPrices: Query<SKU, GetPricesResponse>
        SavePrices: Command<Prices>
        MarkAsSoldOut: Command<PriceCommand>
        RemoveListPrice: Command<PriceCommand>
    } with
        interface IApi

type RootApi = {
    Catalog: CatalogApi
    Home: HomeApi
    Prices: PricesApi
}