namespace Shopfoo.Product

open System
open System.Net.Http
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects.Dependencies
open Shopfoo.Product.Data
open Shopfoo.Product.Workflows
open Shopfoo.Product.Workflows.Instructions

[<Interface>]
type IProductApi =
    abstract member GetProducts: (unit -> Async<Product list>)
    abstract member GetProduct: (SKU -> Async<Product option>)
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)

    abstract member GetPrices: (SKU -> Async<Prices option>)
    abstract member SavePrices: (Prices -> Async<Result<unit, Error>>)
    abstract member MarkAsSoldOut: (SKU -> Async<Result<unit, Error>>)
    abstract member RemoveListPrice: (SKU -> Async<Result<unit, Error>>)

    abstract member AdjustStock: (Stock -> Async<Result<unit, Error>>)
    abstract member DetermineStock: (SKU -> Async<Result<Stock, Error>>)
    abstract member GetSales: (SKU -> Async<Sale list option>)

type internal Api(interpreterFactory: IInterpreterFactory) =
    let interpret = interpreterFactory.Create(ProductDomain)

    let runEffect (productEffect: IProductEffect<_>) =
        match productEffect.Instruction with
        | GetPrices query -> interpret.Query(query, Prices.Client.getPrices)
        | GetSales query -> interpret.Query(query, Sales.Client.getSales)
        | GetStockEvents query -> interpret.Query(query, Warehouse.Client.getStockEvents)
        | SavePrices command -> interpret.Command(command, Prices.Client.savePrices)
        | SaveProduct command -> interpret.Command(command, Catalog.Client.saveProduct)

    let interpretWorkflow (workflow: ProductWorkflow<'arg, 'ret>) args =
        interpret.Workflow runEffect workflow args

    interface IProductApi with
        member val GetProducts = Catalog.Client.getProducts
        member val GetProduct = Catalog.Client.getProduct
        member val SaveProduct = interpretWorkflow SaveProductWorkflow.Instance

        member val GetPrices = Prices.Client.getPrices
        member val SavePrices = interpretWorkflow SavePricesWorkflow.Instance
        member val MarkAsSoldOut = interpretWorkflow MarkAsSoldOutWorkflow.Instance
        member val RemoveListPrice = interpretWorkflow RemoveListPriceWorkflow.Instance

        member val AdjustStock = Warehouse.Client.adjustStock
        member val DetermineStock = interpretWorkflow DetermineStockWorkflow.Instance
        member val GetSales = Sales.Client.getSales

module DependencyInjection =
    open Shopfoo.Data.DependencyInjection
    open Shopfoo.Product.Data.OpenLibrary
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Options

    module OpenLibrary =
        [<Literal>]
        let SectionName = "OpenLibrary"

        [<CLIMutable>]
        type Settings = { BaseUrl: string }

    type IServiceCollection with
        member private services.AddOpenLibraryClient() =
            services.AddHttpClient<OpenLibraryClient>(fun (sp: IServiceProvider) (client: HttpClient) ->
                let openLibrarySettings = sp.GetRequiredService<IOptions<OpenLibrary.Settings>>()
                client.BaseAddress <- Uri(openLibrarySettings.Value.BaseUrl)
                client.AcceptJson()
            )
            |> ignore

            services

        member services.AddProductApi() =
            services // ↩
                .AddOpenLibraryClient()
                .AddSingleton<IProductApi, Api>()
