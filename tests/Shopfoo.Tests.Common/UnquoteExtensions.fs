[<AutoOpen>]
module Shopfoo.Tests.Common.UnquoteExtensions

open Swensen.Unquote

/// <summary>
/// Same as <c>test &lt;@ assertion @&gt;</c> that adds an assertion message in the output when the test fails.
/// </summary>
///
/// <example>
/// <code>
/// let actual, expected = -12, 9
/// testThat "actual should equal expected" &lt;@ actual = expected @&gt;
///
/// </code>
///
/// 💻 Outputs:
/// <code>
/// Xunit.Sdk.TrueException
///
/// message; actual = expected
/// "actual should equal expected"; actual = expected
/// actual = expected
/// -12 = 9
/// false
///
/// Expected: True
/// Actual:   False
/// </code>
/// </example>
let inline testThat message assertion =
    // Notes:
    // - `let _ = message in` allows to "print" the message in the output
    // - `%assertion` uses the slice operation `%` to combine the 2 code quotations
    //   🔗 https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/code-quotations#splicing-operators
    test <@ let _ = message in %assertion @>

/// <summary>
/// Alias for Unquote's <c>test &lt;@ assertion @&gt;</c>, expressing a pre-condition (Arrange/Act guard) rather than the final assertion.
/// </summary>
let inline assume assertion = test assertion

/// <summary>
/// Same as <c>testThat</c> to use for assumptions, i.e. to verify test pre-conditions
/// </summary>
let inline assumeThat message assertion = testThat message assertion