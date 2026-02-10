namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Program
open Shopfoo.Program.Runner

/// <summary>
/// Set of instructions for the "Product" domain.
/// </summary>
/// <remarks>
/// <para>Follow the core of the Tagless Final pattern and define the domain algebra.</para>
/// <para>Includes both query and command instructions, with undo support for commands.</para>
/// </remarks>
[<Interface>]
type IProductInstructions =
    inherit IProgramInstructions

    // === Query Instructions ===
    abstract member GetPrices: (SKU -> Async<Prices option>)
    abstract member GetSales: (SKU -> Async<Sale list option>)
    abstract member GetStockEvents: (SKU -> Async<StockEvent list option>)

    // === Command Instructions ===
    abstract member SavePrices: (Prices -> Async<Result<unit, Error>>)
    abstract member SaveProduct: (Product -> Async<Result<unit, Error>>)
    abstract member AddPrices: (Prices -> Async<Result<unit, Error>>)
    abstract member AddProduct: (Product -> Async<Result<unit, Error>>)

    // === Undo Operations (for Saga pattern) ===
    abstract member DeletePrices: (SKU -> Async<Result<unit, Error>>)
    abstract member DeleteProduct: (SKU -> Async<Result<unit, Error>>)

[<AutoOpen>]
module internal Internals =
    [<RequireQualifiedAccess>]
    module Program =
        type private DefineProgram = DefineProgram<IProductInstructions>

        let getPrices sku = DefineProgram.instruction _.GetPrices(sku)
        let getSales sku = DefineProgram.instruction _.GetSales(sku)
        let getStockEvents sku = DefineProgram.instruction _.GetStockEvents(sku)
        let savePrices prices = DefineProgram.instruction _.SavePrices(prices)
        let saveProduct product = DefineProgram.instruction _.SaveProduct(product)
        let addPrices prices = DefineProgram.instruction _.AddPrices(prices)
        let addProduct product = DefineProgram.instruction _.AddProduct(product)

    [<Interface>]
    type IProductWorkflow<'arg, 'ret> =
        inherit IProgramWorkflow<IProductInstructions, 'arg, 'ret>