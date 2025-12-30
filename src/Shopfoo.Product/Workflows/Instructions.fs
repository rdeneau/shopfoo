module Shopfoo.Product.Workflows.Instructions

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Domain.Types.Warehouse
open Shopfoo.Effects

type GetPricesQuery<'a> = Query<SKU, Prices, 'a>
type GetSalesQuery<'a> = Query<SKU, Sale list, 'a>
type GetStockEventsQuery<'a> = Query<SKU, StockEvent list, 'a>
type SavePricesCommand<'a> = Command<Prices, 'a>
type SaveProductCommand<'a> = Command<Product, 'a>

type ProductInstruction<'a> =
    | GetPrices of GetPricesQuery<'a>
    | GetSales of GetSalesQuery<'a>
    | GetStockEvents of GetStockEventsQuery<'a>
    | SavePrices of SavePricesCommand<'a>
    | SaveProduct of SaveProductCommand<'a>

[<Interface>]
type IProductEffect<'a> =
    inherit IProgramEffect<'a>
    inherit IInterpretableEffect<ProductInstruction<'a>>

type GetPricesEffect<'a>(command: GetPricesQuery<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = GetPricesEffect(command.Map f)
        override val Instruction = GetPrices command

type GetSalesEffect<'a>(command: GetSalesQuery<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = GetSalesEffect(command.Map f)
        override val Instruction = GetSales command

type GetStockEventsEffect<'a>(command: GetStockEventsQuery<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = GetStockEventsEffect(command.Map f)
        override val Instruction = GetStockEvents command

type SavePricesEffect<'a>(command: SavePricesCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SavePricesEffect(command.Map f)
        override val Instruction = SavePrices command

type SaveProductEffect<'a>(command: SaveProductCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SaveProductEffect(command.Map f)
        override val Instruction = SaveProduct command

[<RequireQualifiedAccess>]
module Program =
    let getPrices = Program.effect GetPricesEffect GetPricesQuery
    let getSales = Program.effect GetSalesEffect GetSalesQuery
    let getStockEvents = Program.effect GetStockEventsEffect GetStockEventsQuery
    let savePrices = Program.effect SavePricesEffect SavePricesCommand
    let saveProduct = Program.effect SaveProductEffect SaveProductCommand