module Shopfoo.Product.Workflows.Instructions

open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Sales
open Shopfoo.Effects

type SaveProductCommand<'a> = Command<Product, 'a>
type SavePricesCommand<'a> = Command<Prices, 'a>

type ProductInstruction<'a> =
    | SaveProduct of SaveProductCommand<'a>
    | SavePrices of SavePricesCommand<'a>

[<Interface>]
type IProductEffect<'a> =
    inherit IProgramEffect<'a>
    inherit IInterpretableEffect<ProductInstruction<'a>>

type SaveProductEffect<'a>(command: SaveProductCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SaveProductEffect(command.Map f)
        override val Instruction = SaveProduct command

type SavePricesEffect<'a>(command: SavePricesCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SavePricesEffect(command.Map f)
        override val Instruction = SavePrices command

[<RequireQualifiedAccess>]
module Program =
    let saveProduct args =
        Effect(SaveProductEffect(SaveProductCommand("SaveProduct", args, Stop)))

    let savePrices args =
        Effect(SavePricesEffect(SavePricesCommand("SavePrices", args, Stop)))