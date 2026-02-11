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
    let prepareInstructions (x: IInstructionPreparer<'ins>) =
        { new IProductInstructions with
            member _.GetPrices = x.Prepare pricesPipeline.GetPrices _.Query("GetPrices")
            member _.GetSales = x.Prepare salesPipeline.GetSales _.Query("GetSales")
            member _.GetStockEvents = x.Prepare warehousePipeline.GetStockEvents _.Query("GetStockEvents")

            // TODO RDE: add undo operations
            member _.SavePrices = x.Prepare pricesPipeline.SavePrices _.Command("SavePrices").NoUndo()
            member _.SaveProduct = x.Prepare catalogPipeline.SaveProduct _.Command("SaveProduct").NoUndo()
            member _.AddPrices = x.Prepare pricesPipeline.AddPrices _.Command("AddPrices").NoUndo()
            member _.AddProduct = x.Prepare catalogPipeline.AddProduct _.Command("AddProduct").NoUndo()

            // TODO RDE: to remove once the pipeline functions are used in the undo operations above
            member _.DeletePrices = x.Prepare pricesPipeline.DeletePrices _.Command("DeletePrices").NoUndo()
            member _.DeleteProduct = x.Prepare catalogPipeline.DeleteProduct _.Command("DeleteProduct").NoUndo()
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