namespace Shopfoo.Product.Workflows

open Shopfoo.Domain.Types.Errors
open Shopfoo.Effects

type ProductDomain =
    | ProductDomain

    interface IDomain with
        member _.Name = "Product"

[<AbstractClass>]
type ProductWorkflow<'arg, 'ret>() =
    abstract member Run: 'arg -> Program<Result<'ret, Error>>

    interface IDomainWorkflow<ProductDomain> with
        member val Domain = ProductDomain

    interface IProgramWorkflow<'arg, 'ret> with
        member this.Run arg = this.Run arg
