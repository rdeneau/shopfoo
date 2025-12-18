namespace Shopfoo.Catalog

open Shopfoo.Catalog.Data
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Products
open Shopfoo.Effects.Dependencies
open Shopfoo.Product.Workflows
open Shopfoo.Product.Workflows.Instructions

[<Interface>]
type ICatalogApi =
    abstract member GetProducts: unit -> Async<Result<Product list, Error>>
    abstract member GetProduct: sku: SKU -> Async<Result<Product option, Error>>
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)

type internal Api(interpreterFactory: IInterpreterFactory) =
    let interpret =
        interpreterFactory.Create(ProductDomain)

    let runEffect (productEffect: IProductEffect<_>) =
        match productEffect.Instruction with
        | SaveProduct command -> interpret.Command(command, Products.Client.saveProduct)

    let interpretWorkflow (workflow: ProductWorkflow<'arg, 'ret>) args =
        interpret.Workflow runEffect workflow args

    interface ICatalogApi with
        member _.GetProducts() = Products.Client.getProducts()
        member _.GetProduct(sku) = Products.Client.getProduct sku
        member val SaveProduct = interpretWorkflow (SaveProductWorkflow())

module DependencyInjection =
    open Microsoft.Extensions.DependencyInjection

    type IServiceCollection with
        member services.AddCatalogApi() = // ↩
            services.AddSingleton<ICatalogApi, Api>()