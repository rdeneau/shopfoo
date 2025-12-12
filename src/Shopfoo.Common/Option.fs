[<RequireQualifiedAccess>]
module Shopfoo.Common.Option

/// <summary>
/// Wrap the <paramref name="value"/> if it's a <paramref name="success"/>.
/// </summary>
/// <param name="success">Flag indicating the validity of the value</param>
/// <param name="value">Value to wrap</param>
/// <remarks>
/// 💡 Useful as a bridge between .NET <c>TryXxx()</c> method (like <c>int.TryParse</c>)
/// and F# <see cref="Option"/> type.
/// </remarks>
let ofPair (success, value) =
    match success with
    | true -> Some value
    | false -> None

/// <summary>
/// Create a <c>unit option</c> from the given <paramref name="success"/> flag.
/// </summary>
/// <param name="success">Flag</param>
/// <remarks>
/// 💡 Useful to build partial active pattern before F# 9: see the example.
/// </remarks>
/// <example>
/// <code>let (|Satisfy|_|) predicate x = predicate x |> Option.ofBool</code>
/// </example>
let ofBool success = ofPair (success, ())