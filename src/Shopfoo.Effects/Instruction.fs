/// Helpers to simplify the definition of the domain instructions: programs containing a single step
module Shopfoo.Effects.Instruction

open Microsoft.FSharp.Reflection
open Shopfoo.Domain.Types.Errors

[<Sealed>]
type Instruction<'req, 'res>(request: 'req) =
    member val Request = request

type Command<'req> = Instruction<'req, Result<unit, Error>>
type Query<'req, 'res> = Instruction<'req, 'res option>
type QueryFailable<'req, 'res> = Instruction<'req, Result<'res, Error>>

[<Sealed>]
type StepImpl<'union, 'req, 'res>(instruction: Instruction<'req, 'res>, unionCase: Instruction<'req, 'res> -> 'union) =
    let union = unionCase instruction
    let info, _ = FSharpValue.GetUnionFields(union, typeof<'union>)
    let name = info.Name

    interface IStep with
        member _.Name = name

    interface IInterpretableStep<'union> with
        member _.AsUnion = union

    member this.ToProgram() : Program<'res> = // ↩
        Step(this, fun result -> End(unbox<'res> result))

[<Sealed>]
type StepImplBuilder<'union> =
    static member lift
        (unionCase: Instruction<'req, 'res> -> 'union)
        (instructionCtor: 'req -> Instruction<'req, 'res>)
        (request: 'req)
        : Program<'res> =
        StepImpl<'union, 'req, 'res>(instructionCtor request, unionCase).ToProgram()