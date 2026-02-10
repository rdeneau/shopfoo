namespace Shopfoo.Program

open Shopfoo.Common
open Shopfoo.Domain.Types.Errors

/// Marker interface to identify the set of instructions for a program.
type IProgramInstructions = interface end

/// Type alias to declare the constraint `'ins when 'ins :> IProgramInstructions`.
/// This also allows us to change the constraint if needed without having to update all the code.
type Instructions<'ins when 'ins :> IProgramInstructions> = 'ins

/// <summary>
/// A program that, given a set of instructions (<c>'ins</c>), produces an async result.
/// </summary>
/// <remarks>
/// <para>
/// Programs are written using the <c>program</c> computation expression and executed
/// by providing the instructions that implement <c>IProgramInstructions</c>.
/// </para>
/// <para>
/// This is a minimalist implementation of the ReaderT monad (as a function signature) where the environment is actually the instructions.
/// </para>
/// <para>
/// This replaces the Free monad pattern with a more direct, compositional approach.
/// </para>
/// <para>
/// Parallelism is expressed using <c>let! ... and! ...</c> syntax, which compiles into asynchronous tasks run in parallel (see <c>Program.map2</c>).
/// </para>
/// </remarks>
type Program<'ins, 'ret when Instructions<'ins>> = 'ins -> Async<'ret>

[<RequireQualifiedAccess>]
module Program =
    let retn (a: 'a) : Program<'ins, 'a> = // ↩
        fun _ -> async { return a }

    let bind f (prog: Program<'ins, 'a>) : Program<'ins, 'b> =
        fun ins ->
            async {
                let! a = prog ins
                return! f a ins
            }

    let bindResult (fa: 'a -> Program<'ins, _>) (fe: 'e -> 'err) (result: Result<'a, _>) : Program<'ins, _> =
        match result with
        | Ok a -> fa a
        | Error e -> retn (Error(fe e))

    let map (f: 'a -> 'b) (prog: Program<'ins, 'a>) : Program<'ins, 'b> =
        fun ins ->
            async {
                let! x = prog ins
                return f x
            }

    let map2 (f: 'a -> 'b -> 'c) (progA: Program<'ins, 'a>) (progB: Program<'ins, 'b>) : Program<'ins, 'c> =
        fun ins ->
            async {
                let! childTaskA = Async.StartChild(progA ins)
                let! b = progB ins
                let! a = childTaskA
                return f a b
            }

    let mapError (f: 'error -> Error) (prog: Program<'ins, Result<'a, 'error>>) : Program<'ins, Result<'a, Error>> =
        fun ins ->
            async {
                let! result = prog ins
                return result |> Result.mapError f
            }

    let mapResult (f: 'a -> 'b) (prog: Program<'ins, Result<'a, 'error>>) : Program<'ins, Result<'b, 'error>> = map (Result.map f) prog

    let defaultValue (value: 'a) (prog: Program<'ins, 'a option>) : Program<'ins, 'a> = map (Option.defaultValue value) prog

    let ignore (prog: Program<'ins, Result<_, 'error>>) : Program<'ins, Result<unit, 'error>> = prog |> map Result.ignore

    let requireSome (info: string) (prog: Program<'ins, 'a option>) : Program<'ins, Result<'a, Error>> =
        fun ins ->
            async {
                let! value = prog ins
                return Result.requireSome info value |> liftDataRelatedError
            }

    let requireSomeData (info: string, typeName: TypeName<'a>) (prog: Program<'ins, 'a option>) : Program<'ins, Result<'a, Error>> =
        fun ins ->
            async {
                let! value = prog ins
                return Result.requireSomeData (info, typeName) value |> liftDataRelatedError
            }

    /// Run the given command and return the eventual error without blocking the program
    let runCommandWithNonBlockingError (prog: Program<'ins, Result<unit, Error>>) : Program<'ins, Error option> =
        fun ins ->
            async {
                let! result = prog ins

                return
                    match result with
                    | Ok() -> None
                    | Error e -> Some e
            }

[<AutoOpen>]
module ProgramBuilder =
    /// Bind operator
    let private (>>=) (prog: Program<'ins, 'a>) f : Program<'ins, 'b> = Program.bind f prog

    let private bindResult f = Program.bindResult f id
    let private bindValidation f = Program.bindResult f (Error.Validation >> Error)

    type ProgramBuilder() =
        member _.Return(a: 'a) : Program<'ins, 'a> = Program.retn a
        member _.ReturnFrom(prog: Program<'ins, 'a>) : Program<'ins, 'a> = prog

        member _.Bind(prog: Program<'ins, 'a>, f: 'a -> Program<'ins, 'b>) = Program.bind f prog
        member _.Bind(progR: Program<'ins, Result<_, _>>, f) = progR >>= (bindResult f)
        member _.Bind(result: Result<_, _>, f) = result |> bindResult f // Useful to bind the result of a domain type smart constructor

        member _.Bind2Return(progA: Program<'ins, 'a>, progB: Program<'ins, 'b>, f: 'a * 'b -> 'c) : Program<'ins, 'c> =
            Program.map2 (fun a b -> f (a, b)) progA progB

        member _.MergeSources(progA: Program<'ins, 'a>, progB: Program<'ins, 'b>) : Program<'ins, 'a * 'b> =
            Program.map2 (fun a b -> (a, b)) progA progB

    let program = ProgramBuilder()

[<AutoOpen>]
module ProgramBuilderExtensions =
    type ProgramBuilder with
        // Overloads to bind `Result<'a, XxxError>` and lift the error part to `Error`

        member inline x.Bind(result: Result<_, DataRelatedError>, f) = x.Bind(liftDataRelatedError result, f)
        member inline x.Bind(result: Result<_, OperationNotAllowedError>, f) = x.Bind(liftOperationNotAllowed result, f)
        member inline x.Bind(guardClause: Result<_, GuardClauseError>, f) = x.Bind(liftGuardClause guardClause, f)
        member inline x.Bind(validation: Validation<_, GuardClauseError>, f) = x.Bind(liftValidation validation, f)

        // Overloads to bind `Program<Result<'a, XxxError>>` and lift the error part to `Error`

        member inline x.Bind(progR: Program<'ins, Result<_, DataRelatedError>>, f) = // ↩
            x.Bind(progR = (progR |> Program.mapError DataError), f = f)

        member inline x.Bind(progR: Program<'ins, Result<_, OperationNotAllowedError>>, f) = // ↩
            x.Bind(progR = (progR |> Program.mapError OperationNotAllowed), f = f)

        member inline x.Bind(progR: Program<'ins, Result<_, GuardClauseError>>, f) = // ↩
            x.Bind(progR = (progR |> Program.mapError GuardClause), f = f)

        member inline x.Bind(progV: Program<'ins, Validation<_, GuardClauseError>>, f) = // ↩
            x.Bind(progR = (progV |> Program.map liftValidation), f = f)

/// Static class to help defining a program for a single instruction.
/// Then, for every instruction in the "algrebra" (interface that inherits <c>IProgramInstructions</c>), we can define a function
/// using <c>DefineProgram.instruction</c> and use this function in the workflow written with the <c>program</c> computation expression.
type DefineProgram<'ins when Instructions<'ins>> =
    /// <summary>
    /// This function is an identity function (like <c>id</c>) used for DevExp purposes to help defining a program from an instruction
    /// picked up from the intellisense suggestions and using shorthand lambda syntax for the lambda parameter.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// type private DefineProgram = DefineProgram&lt;IProductInstructions>
    /// let getPrices sku = DefineProgram.instruction _.GetPrices(sku)
    /// let getSales sku = DefineProgram.instruction _.GetSales(sku)
    /// let savePrices prices = DefineProgram.instruction _.SavePrices(prices)
    /// </code>
    /// </example>
    /// <remarks>
    /// Don't forget to use parentheses around the instruction parameter for the shorthand lambda syntax to work:
    /// <br /> - ✅ Correct: <c>DefineProgram.instruction _.GetPrices(sku)</c>
    /// <br /> - ❌ Incorrect: <c>DefineProgram.instruction _.GetPrices sku</c> (does not compile)
    /// </remarks>
    static member inline instruction([<InlineIfLambda>] work: 'ins -> Async<'ret>) : Program<'ins, 'ret> = work