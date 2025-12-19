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
    abstract member GetProducts: (unit -> Async<Result<Product list, Error>>)
    abstract member GetProduct: (SKU -> Async<Result<Product option, Error>>)
    abstract member GetPrices: (SKU -> Async<Result<Prices option, Error>>)
    abstract member GetSales: (SKU -> Async<Result<Sale list, Error>>)
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)
    abstract member SavePrices: (Prices -> Async<Result<unit, Error>>)

type internal Api(interpreterFactory: IInterpreterFactory) =
    let interpret = interpreterFactory.Create(ProductDomain)

    let runEffect (productEffect: IProductEffect<_>) =
        match productEffect.Instruction with
        | SaveProduct command -> interpret.Command(command, Catalog.Client.saveProduct)
        | SavePrices command -> interpret.Command(command, Prices.Client.savePrices)

    let interpretWorkflow (workflow: ProductWorkflow<'arg, 'ret>) args =
        interpret.Workflow runEffect workflow args

    interface IProductApi with
        member val GetProducts = Catalog.Client.getProducts
        member val GetProduct = Catalog.Client.getProduct
        member val GetPrices = Prices.Client.getPrices
        member val GetSales = Sales.Client.getSales
        member val SaveProduct = interpretWorkflow (SaveProductWorkflow())
        member val SavePrices = interpretWorkflow (SavePricesWorkflow())

module DependencyInjection =
    open Microsoft.Extensions.DependencyInjection

    type IServiceCollection with
        member services.AddCatalogApi() = // ↩
            services.AddSingleton<IProductApi, Api>()