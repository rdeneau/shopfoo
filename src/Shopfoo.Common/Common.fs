[<AutoOpen>]
module Shopfoo.Common.Common

open System.Collections.Generic

/// <summary>
/// Create a <c>Dictionary</c> from the given <paramref name="items"/>,
/// using <paramref name="getKey"/> to determine the key for each item.
/// </summary>
/// <param name="getKey">Determine the key for each item.</param>
/// <param name="items">Items to put in the dictionary</param>
let dictionaryBy getKey items =
    dict [ for item in items -> getKey item, item ] |> Dictionary