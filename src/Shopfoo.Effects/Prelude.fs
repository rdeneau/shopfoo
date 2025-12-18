namespace Shopfoo.Effects

open Shopfoo.Domain.Types.Errors

/// <summary>
/// Identify an effect that can be inserted in a program.
/// </summary>
/// <remarks>
/// The <c>Map</c> method satisfies the <em>Functor</em> laws:
/// <br/> - <em>Identity:</em> <c>effect.Map(id)</c> ≡ <c>effect</c>
/// <br/> - <em>Composition:</em> <c>effect.Map(f >> g)</c> ≡ <c>effect.Map(f).Map(g)</c>
/// </remarks>
[<Interface>]
type IProgramEffect<'a> =
    abstract member Map: f: ('a -> 'b) -> IProgramEffect<'b>

/// <summary>
/// An individual effectful instruction.
/// </summary>
/// <remarks>
/// - The <c>Instruction</c> property should return a case of a union type gathering a set of instructions.<br/>
/// - It will be used while interpreting the program to get the exhaustive pattern matching of the instructions supported by the program.
/// </remarks>
[<Interface>]
type IInterpretableEffect<'union> =
    abstract member Instruction: 'union

/// <summary>
/// Base type for an instruction (a Command or a Query) wrapped in an effect.
/// </summary>
/// <remarks>
/// An instruction is defined only by 2 things: its argument(s) and a continuation function
/// taking the result of the instruction and passing it to the rest of the program. <br/>
/// The gap between them is bridged at the interpreter level, which is responsible for
/// passing the right handler to the instruction's <c>Accept</c> method.
/// </remarks>
[<Sealed>]
type Instruction<'arg, 'ret, 'a>
    /// <summary>
    /// Constructs an instruction effect with the given argument(s) and the continuation function.
    /// </summary>
    /// <param name="name">Name of the instruction, usable for logging or debugging</param>
    /// <param name="arg">Argument(s) for this instruction.</param>
    /// <param name="cont">Continuation function to the next effect(s).</param>
    /// <remarks>
    /// <para>
    /// For an effect in a <c>Program</c>, the continuation is initially the <c>Pure</c> case of the <c>Program</c>,
    /// and then is built by composition in the effect <c>Map</c> method.
    /// </para>
    /// <para>
    /// To preserve encapsulation, the <c>arg</c> and <c>cont</c> parameters are not exposed as public properties.
    /// You should use the <c>Run</c> or <c>RunAsync</c> methods to execute the instruction with the given handler.
    /// </para>
    /// </remarks>
    (name: string, arg: 'arg, cont: 'ret -> 'a) =
    /// Name of the instruction, usable for logging or debugging
    member val Name = name

    /// <summary>
    /// Chain the continuation with the given function <c>f</c>.
    /// </summary>
    member _.Map(f: 'a -> 'b) = Instruction(name, arg, cont >> f)

    /// <summary>
    /// Call the given <paramref name="runner"/> with the predefined argument(s) of this instruction,
    /// and pass the result to its continuation function.
    /// </summary>
    /// <param name="runner">function generating the returned value of this instruction</param>
    /// <remarks>
    /// Use <c>RunAsync</c> method given an asynchronous runner.
    /// </remarks>
    member _.Run(runner) =
        let ret = runner arg
        cont ret

    /// <summary>
    /// Call the given <paramref name="asyncRunner"/> with the predefined argument(s) of this instruction,
    /// and pass the result to its continuation function.
    /// </summary>
    /// <param name="asyncRunner">function generating asynchronously the returned value of this instruction</param>
    /// <remarks>
    /// Use <c>Run</c> method given a synchronous runner.
    /// </remarks>
    member _.RunAsync(asyncRunner) =
        async {
            let! ret = asyncRunner arg
            return cont ret
        }

/// <summary>
/// Alias for an instruction either succeeding and returning no value, or failing and returning a common error (*)<br/>
/// (*) from our <c>Shopfoo.Domain.Types.Errors</c> namespace
/// </summary>
type Command<'arg, 'a> = Instruction<'arg, Result<unit, Error>, 'a>

/// Alias for an instruction returning an optional value.
type Query<'arg, 'ret, 'a> = Instruction<'arg, 'ret option, 'a>

/// <summary>
/// Alias for an instruction either succeeding and returning a value, or failing and returning a common error (*)<br/>
/// (*) from our <c>Shopfoo.Domain.Types.Errors</c> namespace
/// </summary>
type QueryFailable<'arg, 'ret, 'a> = Instruction<'arg, Result<'ret, Error>, 'a>