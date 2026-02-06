namespace Shopfoo.Product

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects.Dependencies
open Shopfoo.Product.Data.Catalog
open Shopfoo.Product.Data.OpenLibrary
open Shopfoo.Product.Data.Prices
open Shopfoo.Product.Data.Sales
open Shopfoo.Product.Data.Warehouse
open Shopfoo.Product.Workflows
open Shopfoo.Product.Workflows.Instructions

[<Interface>]
type IProductApi =
    abstract member GetProducts: (Provider -> Async<Product list>)
    abstract member GetProduct: (SKU -> Async<Product option>)
    abstract member AddProduct: (Product -> Async<Result<unit, Error>>)
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)

    abstract member GetPrices: (SKU -> Async<Prices option>)
    abstract member SavePrices: (Prices -> Async<Result<unit, Error>>)
    abstract member MarkAsSoldOut: (SKU -> Async<Result<unit, Error>>)
    abstract member RemoveListPrice: (SKU -> Async<Result<unit, Error>>)

    abstract member AdjustStock: (Stock -> Async<Result<unit, Error>>)
    abstract member DetermineStock: (SKU -> Async<Result<Stock, Error>>)
    abstract member GetSales: (SKU -> Async<Sale list option>)

    abstract member SearchAuthors: (string -> Async<Result<BookAuthorSearchResults, Error>>)
    abstract member SearchBooks: (string -> Async<Result<BookSearchResults, Error>>)

type internal Api
    (
        interpreterFactory: IInterpreterFactory,
        catalogPipeline: CatalogPipeline,
        openLibraryPipeline: OpenLibraryPipeline,
        pricesPipeline: PricesPipeline,
        salesPipeline: SalesPipeline,
        warehousePipeline: WarehousePipeline
    ) =
    let interpret = interpreterFactory.Create(ProductDomain)

    let runEffect (productEffect: IProductEffect<_>) =
        match productEffect.Instruction with
        | GetPrices query -> interpret.Query(query, pricesPipeline.GetPrices)
        | GetSales query -> interpret.Query(query, salesPipeline.GetSales)
        | GetStockEvents query -> interpret.Query(query, warehousePipeline.GetStockEvents)
        | SavePrices command -> interpret.Command(command, pricesPipeline.SavePrices)
        | SaveProduct command -> interpret.Command(command, catalogPipeline.SaveProduct)
        | AddPrices command -> interpret.Command(command, pricesPipeline.AddPrices)
        | AddProduct command -> interpret.Command(command, catalogPipeline.AddProduct)

    let interpretWorkflow (workflow: ProductWorkflow<'arg, 'ret>) args = // ↩
        interpret.Workflow runEffect workflow args

    interface IProductApi with
        member val GetProducts = catalogPipeline.GetProducts
        member val GetProduct = catalogPipeline.GetProduct
        member val SaveProduct = interpretWorkflow SaveProductWorkflow.Instance
        member val AddProduct = interpretWorkflow AddProductWorkflow.Instance

        member val GetPrices = pricesPipeline.GetPrices
        member val SavePrices = interpretWorkflow SavePricesWorkflow.Instance
        member val MarkAsSoldOut = interpretWorkflow MarkAsSoldOutWorkflow.Instance
        member val RemoveListPrice = interpretWorkflow RemoveListPriceWorkflow.Instance

        member val GetSales = salesPipeline.GetSales

        member val AdjustStock = warehousePipeline.AdjustStock
        member val DetermineStock = interpretWorkflow DetermineStockWorkflow.Instance

        member val SearchAuthors = openLibraryPipeline.SearchAuthors
        member val SearchBooks = openLibraryPipeline.SearchBooks