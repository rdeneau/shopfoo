[<RequireQualifiedAccess>]
module Shopfoo.Common.Seq

/// <summary>
/// Split a sequence into chunks, where a new chunk starts each time the <paramref name="predicate"/> returns <c>true</c>.
/// </summary>
/// <param name="predicate">identify the first item of new chunks</param>
/// <param name="items">sequence to split in chunks</param>
let splitWhen (predicate: 'T -> bool) (items: 'T seq) : 'T array seq =
    seq {
        let e = items.GetEnumerator()

        if e.MoveNext() then
            let a = ResizeArray()
            a.Add(e.Current)

            while e.MoveNext() do
                if predicate e.Current then
                    yield a.ToArray()
                    a.Clear()

                a.Add(e.Current)

            yield a.ToArray()
    }