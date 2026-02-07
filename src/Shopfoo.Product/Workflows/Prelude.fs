namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Program

/// <summary>
/// Set of instructions for the "Product" domain.
/// </summary>
/// <remarks>
/// <para>Follow the core of the Tagless Final pattern and define the domain algebra.</para>
/// <para>Define the domain algebra.</para>
/// </remarks>
[<Interface>]
type IProductInstructions =
    inherit IProgramInstructions
    abstract member GetPrices: (SKU -> Async<Prices option>)
    abstract member GetSales: (SKU -> Async<Sale list option>)
    abstract member GetStockEvents: (SKU -> Async<StockEvent list option>)
    abstract member SavePrices: (Prices -> Async<Result<unit, Error>>)
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)
    abstract member AddPrices: (Prices -> Async<Result<unit, Error>>)
    abstract member AddProduct: (Product -> Async<Result<unit, Error>>)

[<AutoOpen>]
module internal Internals =
    [<RequireQualifiedAccess>]
    module Program =
        let inline private run (work: IProductInstructions -> Async<'ret>) = work

        let getPrices sku = run _.GetPrices(sku)
        let getSales sku = run _.GetSales(sku)
        let getStockEvents sku = run _.GetStockEvents(sku)
        let savePrices prices = run _.SavePrices(prices)
        let saveProduct product = run _.SaveProduct(product)
        let addPrices prices = run _.AddPrices(prices)
        let addProduct product = run _.AddProduct(product)

    [<Interface>]
    type IProductWorkflow<'arg, 'ret> =
        inherit IProgramWorkflow<IProductInstructions, 'arg, 'ret>