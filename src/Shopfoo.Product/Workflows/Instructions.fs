module Shopfoo.Product.Workflows.Instructions

open Shopfoo.Domain.Types.Products
open Shopfoo.Effects

type SaveProductCommand<'a> = Command<Product, 'a>

type ProductInstruction<'a> = // ↩ TODO
    | SaveProduct of SaveProductCommand<'a>

[<Interface>]
type IProductEffect<'a> =
    inherit IProgramEffect<'a>
    inherit IInterpretableEffect<ProductInstruction<'a>>

type SaveProductEffect<'a>(command: SaveProductCommand<'a>) =
    interface IProductEffect<'a> with
        override _.Map(f) = SaveProductEffect(command.Map f)
        override val Instruction = SaveProduct command

[<RequireQualifiedAccess>]
module Program =
    let saveProduct args =
        Effect(SaveProductEffect(SaveProductCommand("SaveProduct", args, Stop)))
