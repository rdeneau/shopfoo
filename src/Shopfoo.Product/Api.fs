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
open Shopfoo.Program.Runner

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
        workflowRunnerFactory: IWorkflowRunnerFactory,
        catalogPipeline: CatalogPipeline,
        openLibraryPipeline: OpenLibraryPipeline,
        pricesPipeline: PricesPipeline,
        salesPipeline: SalesPipeline,
        warehousePipeline: WarehousePipeline
    ) =
    let prepareInstructions (x: IWorkflowPreparer<'ins>) =
        { new IProductInstructions with
            member _.GetPrices = x.PrepareQuery("GetPrices", pricesPipeline.GetPrices)
            member _.GetSales = x.PrepareQuery("GetSales", salesPipeline.GetSales)
            member _.GetStockEvents = x.PrepareQuery("GetStockEvents", warehousePipeline.GetStockEvents)

            // TODO RDE: add undo operations
            member _.SavePrices = x.PrepareCommand("SavePrices", pricesPipeline.SavePrices)
            member _.SaveProduct = x.PrepareCommand("SaveProduct", catalogPipeline.SaveProduct)
            member _.AddPrices = x.PrepareCommand("AddPrices", pricesPipeline.AddPrices)
            member _.AddProduct = x.PrepareCommand("AddProduct", catalogPipeline.AddProduct)

            // TODO RDE: to remove once the pipeline functions are used in the undo operations above
            member _.DeletePrices = x.PrepareCommand("DeletePrices", pricesPipeline.DeletePrices)
            member _.DeleteProduct = x.PrepareCommand("DeleteProduct", catalogPipeline.DeleteProduct)
        }

    let runWorkflow (workflow: IProductWorkflow<'arg, 'ret>) (arg: 'arg) : Async<Result<'ret, Error>> =
        async {
            let workflowRunner = workflowRunnerFactory.Create(domainName = "Product")
            let! result, _ = workflowRunner.RunInSaga workflow arg prepareInstructions
            // Here we could inspect the saga state and history for debugging or reporting...
            return result
        }

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