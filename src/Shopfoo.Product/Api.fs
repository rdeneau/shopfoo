namespace Shopfoo.Product

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Product.Data.Catalog
open Shopfoo.Product.Data.OpenLibrary
open Shopfoo.Product.Data.Prices
open Shopfoo.Product.Data.Sales
open Shopfoo.Product.Data.Warehouse
open Shopfoo.Product.Workflows
open Shopfoo.Program.Dependencies

[<Interface>]
type IProductApi =
    abstract member GetProducts: (Provider -> Async<Product list>)
    abstract member GetProduct: (SKU -> Async<Product option>)
    abstract member AddProduct: (Product * Currency -> Async<Result<unit, Error>>)
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

[<Sealed>]
type internal Api
    (
        monitorFactory: IDomainMonitorFactory,
        catalogPipeline: CatalogPipeline,
        openLibraryPipeline: OpenLibraryPipeline,
        pricesPipeline: PricesPipeline,
        salesPipeline: SalesPipeline,
        warehousePipeline: WarehousePipeline
    ) =
    let monitor = monitorFactory.Create("Product")

    let instructions =
        { new IProductInstructions with
            member _.GetPrices = monitor.Query "GetPrices" pricesPipeline.GetPrices
            member _.GetSales = monitor.Query "GetSales" salesPipeline.GetSales
            member _.GetStockEvents = monitor.Query "GetStockEvents" warehousePipeline.GetStockEvents
            member _.SavePrices = monitor.Command "SavePrices" pricesPipeline.SavePrices
            member _.SaveProduct = monitor.Command "SaveProduct" catalogPipeline.SaveProduct
            member _.AddPrices = monitor.Command "AddPrices" pricesPipeline.AddPrices
            member _.AddProduct = monitor.Command "AddProduct" catalogPipeline.AddProduct
        }

    let runWorkflow (workflow: IProductWorkflow<'arg, 'ret>) (arg: 'arg) : Async<Result<'ret, Error>> = // ↩
        monitor.Workflow workflow arg instructions

    interface IProductApi with
        member val GetProducts = catalogPipeline.GetProducts
        member val GetProduct = catalogPipeline.GetProduct
        member val SaveProduct = fun product -> runWorkflow SaveProductWorkflow.Instance product
        member val AddProduct = fun (product, currency) -> runWorkflow AddProductWorkflow.Instance (product, currency)

        member val GetPrices = pricesPipeline.GetPrices
        member val SavePrices = fun prices -> runWorkflow SavePricesWorkflow.Instance prices
        member val MarkAsSoldOut = fun sku -> runWorkflow MarkAsSoldOutWorkflow.Instance sku
        member val RemoveListPrice = fun sku -> runWorkflow RemoveListPriceWorkflow.Instance sku

        member val GetSales = salesPipeline.GetSales

        member val AdjustStock = warehousePipeline.AdjustStock
        member val DetermineStock = fun sku -> runWorkflow DetermineStockWorkflow.Instance sku

        member val SearchAuthors = openLibraryPipeline.SearchAuthors
        member val SearchBooks = openLibraryPipeline.SearchBooks