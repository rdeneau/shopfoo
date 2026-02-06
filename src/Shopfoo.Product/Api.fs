namespace Shopfoo.Product

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
        interpreterFactory: IInterpreterFactory, // ↩
        fakeStoreClient: FakeStore.IFakeStoreClient,
        openLibraryClient: OpenLibrary.IOpenLibraryClient,
        saleRepository: Sales.SaleRepository
    ) =
    let interpret = interpreterFactory.Create(ProductDomain)

    let runEffect (productEffect: IProductEffect<_>) =
        match productEffect.Instruction with
        | GetPrices query -> interpret.Query(query, Prices.Pipeline.getPrices)
        | GetSales query -> interpret.Query(query, Sales.Pipeline.getSales saleRepository)
        | GetStockEvents query -> interpret.Query(query, Warehouse.Pipeline.getStockEvents)
        | SavePrices command -> interpret.Command(command, Prices.Pipeline.savePrices)
        | SaveProduct command -> interpret.Command(command, Catalog.Pipeline.saveProduct)
        | AddPrices command -> interpret.Command(command, Prices.Pipeline.addPrices)
        | AddProduct command -> interpret.Command(command, Catalog.Pipeline.addProduct)

    let interpretWorkflow (workflow: ProductWorkflow<'arg, 'ret>) args = // ↩
        interpret.Workflow runEffect workflow args

    interface IProductApi with
        member val GetProducts = Catalog.Pipeline.getProducts fakeStoreClient
        member val GetProduct = Catalog.Pipeline.getProduct openLibraryClient
        member val SaveProduct = interpretWorkflow SaveProductWorkflow.Instance
        member val AddProduct = interpretWorkflow AddProductWorkflow.Instance

        member val GetPrices = Prices.Pipeline.getPrices
        member val SavePrices = interpretWorkflow SavePricesWorkflow.Instance
        member val MarkAsSoldOut = interpretWorkflow MarkAsSoldOutWorkflow.Instance
        member val RemoveListPrice = interpretWorkflow RemoveListPriceWorkflow.Instance

        member val AdjustStock = Warehouse.Pipeline.adjustStock
        member val DetermineStock = interpretWorkflow DetermineStockWorkflow.Instance
        member val GetSales = Sales.Pipeline.getSales saleRepository

        member val SearchAuthors = OpenLibrary.Pipeline.searchAuthors openLibraryClient
        member val SearchBooks = OpenLibrary.Pipeline.searchBooks openLibraryClient