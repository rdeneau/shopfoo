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
type AddProductCommand<'a> = Command<Product, 'a>

type ProductInstruction<'a> =
    | GetPrices of GetPricesQuery<'a>
    | GetSales of GetSalesQuery<'a>
    | GetStockEvents of GetStockEventsQuery<'a>
    | SavePrices of SavePricesCommand<'a>
    | SaveProduct of SaveProductCommand<'a>
    | AddProduct of AddProductCommand<'a>

[<Interface>]
type IProductEffect<'a> =
    inherit IProgramEffect<'a>
    inherit IInterpretableEffect<ProductInstruction<'a>>

type GetPricesEffect<'a>(query: GetPricesQuery<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = GetPricesEffect(query.Map f)
        override val Instruction = GetPrices query

type GetSalesEffect<'a>(query: GetSalesQuery<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = GetSalesEffect(query.Map f)
        override val Instruction = GetSales query

type GetStockEventsEffect<'a>(query: GetStockEventsQuery<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = GetStockEventsEffect(query.Map f)
        override val Instruction = GetStockEvents query

type SavePricesEffect<'a>(command: SavePricesCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SavePricesEffect(command.Map f)
        override val Instruction = SavePrices command

type SaveProductEffect<'a>(command: SaveProductCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SaveProductEffect(command.Map f)
        override val Instruction = SaveProduct command

type AddProductEffect<'a>(command: AddProductCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = AddProductEffect(command.Map f)
        override val Instruction = AddProduct command

[<RequireQualifiedAccess>]
module Program =
    let getPrices = Program.effect GetPricesEffect GetPricesQuery
    let getSales = Program.effect GetSalesEffect GetSalesQuery
    let getStockEvents = Program.effect GetStockEventsEffect GetStockEventsQuery
    let savePrices = Program.effect SavePricesEffect SavePricesCommand
    let saveProduct = Program.effect SaveProductEffect SaveProductCommand
    let addProduct = Program.effect AddProductEffect AddProductCommand