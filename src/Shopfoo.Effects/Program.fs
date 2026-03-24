namespace Shopfoo.Effects

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors

type IProgramStep = interface end

[<Interface>]
type IProgramStepReturning<'res> =
    inherit IProgramStep

[<Interface>]
type IInterpretableStep<'union> =
    abstract member Name: string
    abstract member AsUnion: 'union

type Program<'a> =
    | End of 'a
    | Step of step: IProgramStep * cont: (obj -> Program<'a>)
    | Parallel of left: Program<obj> * right: Program<obj> * merge: (obj * obj -> Program<'a>)

[<AutoOpen>]
module ProgramBuilder =
    let rec private bind f program =
        match program with
        | End x -> f x
        | Step(step, cont) -> Step(step, cont >> bind f)
        | Parallel(left, right, merge) -> Parallel(left, right, merge >> bind f)

    /// Bind operator
    let private (>>=) program f = bind f program

    let private bindResult (f: 'v -> Program<_>) (result: Result<'v, _>) =
        match result with
        | Ok v -> f v
        | Error e -> End(Error e)

    let private bindValidation (f: 'v -> Program<_>) (validation: Result<'v, GuardClauseError list>) : Program<_> =
        match validation with
        | Ok v -> f v
        | Error errors -> End((errors |> Error.Validation) |> Error)

    let rec private map (f: 'a -> 'b) (prog: Program<'a>) : Program<'b> =
        match prog with
        | End x -> End(f x)
        | Step(step, cont) -> Step(step, cont >> map f)
        | Parallel(left, right, merge) -> Parallel(left, right, merge >> map f)

    let map2 (f: 'a -> 'b -> 'c) (progA: Program<'a>) (progB: Program<'b>) : Program<'c> =
        Parallel(map box progA, map box progB, fun (a, b) -> End(f (unbox<'a> a) (unbox<'b> b)))

    type ProgramBuilder() =
        member _.Zero() = End()
        member _.Return(value) = End value
        member _.ReturnFrom(value) = value
        member _.Bind(program: Program<_>, f) = program >>= f
        member _.Bind(programR: Program<Result<_, _>>, f) = programR >>= (bindResult f)
        member _.Bind(result: Result<_, _>, f) = result |> bindResult f // Useful to bind the result of a domain type smart constructor
        member _.Bind2Return(progA, progB, f) = map2 (fun a b -> f (a, b)) progA progB
        member this.MergeSources(progA, progB) = this.Bind2Return(progA, progB, id)

    let program = ProgramBuilder()

[<RequireQualifiedAccess>]
module Program =
    /// No Op program = doing nothing
    let inline noop () = program { return Ok() }

    let inline ok value = program { return Ok value }

    let map (f: 'a -> 'b) (prog: Program<'a>) =
        program {
            let! x = prog
            return f x
        }

    let mapError (f: 'error -> Error) (programR: Program<Result<_, 'error>>) =
        program {
            let! result = programR //here we retrieve return value from our program `prog`. Like async/await in C#
            return result |> Result.mapError f
        }

    let mapResult f programR = map (Result.map f) programR

    let defaultValue value prog = prog |> map (Option.defaultValue value)
    let ignore prog = prog |> map Result.ignore

    let requireSome info (queryResult: Program<'a option>) = queryResult |> map (Result.requireSome info)
    let requireSomeData info (queryResult: Program<'a option>) = queryResult |> map (Result.requireSomeData info)

    /// Run the given command and return the eventual error without blocking the program
    let runCommandWithNonBlockingError (xProg: Program<Result<unit, 'e>>) =
        program {
            let! result = xProg
            return result |> Result.tryGetError
        }

[<AutoOpen>]
module ProgramBuilderExtensions =
    type ProgramBuilder with
        // Overloads to bind `Result<'a, XxxError>` and lift the error part to `Error`

        member inline x.Bind(result: Result<_, DataRelatedError>, f) = x.Bind(liftDataRelatedError result, f)
        member inline x.Bind(result: Result<_, OperationNotAllowedError>, f) = x.Bind(liftOperationNotAllowed result, f)
        member inline x.Bind(guardClause: Result<_, GuardClauseError>, f) = x.Bind(liftGuardClause guardClause, f)
        member inline x.Bind(validation: Validation<_, GuardClauseError>, f) = x.Bind(liftValidation validation, f)

        // Overloads to bind `Program<Result<'a, XxxError>>` and lift the error part to `Error`

        member inline x.Bind(programR: Program<Result<_, DataRelatedError>>, f) = // ↩
            x.Bind(programR = (programR |> Program.mapError DataError), f = f)

        member inline x.Bind(programR: Program<Result<_, OperationNotAllowedError>>, f) = // ↩
            x.Bind(programR = (programR |> Program.mapError OperationNotAllowed), f = f)

        member inline x.Bind(programR: Program<Result<_, GuardClauseError>>, f) = // ↩
            x.Bind(programR = (programR |> Program.mapError GuardClause), f = f)

        member inline x.Bind(programV: Program<Validation<_, GuardClauseError>>, f) = // ↩
            x.Bind(programR = (programV |> Program.map liftValidation), f = f)

[<Interface>]
type IDomain =
    abstract member Name: string

[<Interface>]
type IDomainWorkflow<'dom when 'dom :> IDomain> =
    abstract member Domain: 'dom

[<Interface>]
type IProgramWorkflow<'req, 'res> =
    abstract member Run: 'req -> Program<Result<'res, Error>>