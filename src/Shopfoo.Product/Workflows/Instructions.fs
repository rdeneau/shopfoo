module Shopfoo.Product.Workflows.Instructions

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects.Instruction

// Queries: <Request, Response>
type GetPricesQuery = Query<SKU, Prices>
type GetSalesQuery = Query<SKU, Sale list>
type GetStockEventsQuery = Query<SKU, StockEvent list>

// Commands: <Response>
type SavePricesCommand = Command<Prices>
type SaveProductCommand = Command<Product>
type AddPricesCommand = Command<Prices>
type AddProductCommand = Command<Product>

type ProductInstruction =
    | GetPrices of GetPricesQuery
    | GetSales of GetSalesQuery
    | GetStockEvents of GetStockEventsQuery
    | SavePrices of SavePricesCommand
    | SaveProduct of SaveProductCommand
    | AddPrices of AddPricesCommand
    | AddProduct of AddProductCommand

[<RequireQualifiedAccess>]
module Program =
    type private Step = StepImplBuilder<ProductInstruction>

    let getPrices = Step.lift GetPrices GetPricesQuery
    let getSales = Step.lift GetSales GetSalesQuery
    let getStockEvents = Step.lift GetStockEvents GetStockEventsQuery
    let savePrices = Step.lift SavePrices SavePricesCommand
    let saveProduct = Step.lift SaveProduct SaveProductCommand
    let addPrices = Step.lift AddPrices AddPricesCommand
    let addProduct = Step.lift AddProduct AddProductCommand