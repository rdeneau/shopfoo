namespace Shopfoo.Catalog

open Shopfoo.Catalog.Data
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects.Dependencies
open Shopfoo.Product.Workflows
open Shopfoo.Product.Workflows.Instructions

[<Interface>]
type IProductApi =
    abstract member GetProducts: (unit -> Async<Product list>)
    abstract member GetProduct: (SKU -> Async<Product option>)
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)

    abstract member GetSales: (SKU -> Async<Sale list>)

    abstract member GetPrices: (SKU -> Async<Prices option>)
    abstract member SavePrices: (Prices -> Async<Result<unit, Error>>)
    abstract member RemoveListPrice: (SKU -> Async<Result<unit, Error>>)

type internal Api(interpreterFactory: IInterpreterFactory) =
    let interpret = interpreterFactory.Create(ProductDomain)

    let runEffect (productEffect: IProductEffect<_>) =
        match productEffect.Instruction with
        | GetPrices query -> interpret.Query(query, Prices.Client.getPrices)
        | SavePrices command -> interpret.Command(command, Prices.Client.savePrices)
        | SaveProduct command -> interpret.Command(command, Catalog.Client.saveProduct)

    let interpretWorkflow (workflow: ProductWorkflow<'arg, 'ret>) args =
        interpret.Workflow runEffect workflow args

    interface IProductApi with
        member val GetProducts = Catalog.Client.getProducts
        member val GetProduct = Catalog.Client.getProduct
        member val GetPrices = Prices.Client.getPrices
        member val GetSales = Sales.Client.getSales
        member val RemoveListPrice = interpretWorkflow (RemoveListPriceWorkflow())
        member val SaveProduct = interpretWorkflow (SaveProductWorkflow())
        member val SavePrices = interpretWorkflow (SavePricesWorkflow())

module DependencyInjection =
    open Microsoft.Extensions.DependencyInjection

    type IServiceCollection with
        member services.AddCatalogApi() = // ↩
            services.AddSingleton<IProductApi, Api>()