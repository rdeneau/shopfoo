[<RequireQualifiedAccess>]
module Shopfoo.Common.String

open System
open System.Text
open Shopfoo.Common

[<Literal>]
let empty = ""

/// Check if the string is null or white space
let inline isEmpty s = String.IsNullOrWhiteSpace s

/// Used to have a string not empty, either s or the given defaultValue
let inline defaultIfEmpty defaultValue s = if isEmpty s then defaultValue else s

let inline emptyIfNull s =
    match s with
    | null -> empty
    | _ -> s

let ensureStartsWith prefix s =
    match (emptyIfNull s) with
    | s when s.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase) -> s
    | s -> prefix + s

let trimWhiteSpace s = (emptyIfNull s).Trim()
let trimEndWhiteSpace s = (emptyIfNull s).TrimEnd()

let trimStart (c: char) s = (emptyIfNull s).TrimStart c
let trimEnd (c: char) s = (emptyIfNull s).TrimEnd c

let indexOf (c: char) s = (emptyIfNull s).IndexOf c

let toLower s = (emptyIfNull s).ToLowerInvariant()
let toUpper s = (emptyIfNull s).ToUpperInvariant()

/// From camelCase or PascalCase to kebab-case
let toKebab s =
    trimWhiteSpace s
    |> Seq.splitWhen Char.IsUpper
    |> Seq.map String
    |> String.concat "-"
    |> toLower

let substring start length s =
    match s |> String.length with
    | 0 -> empty
    | n -> s.Substring(start, Math.Min(length, n - start))

let replace (oldValue: string) (newValue: string) s =
    (emptyIfNull s).Replace(oldValue, newValue)

let join (separator: string) (chunks: string seq) = String.Join(separator, chunks)

let split (separator: char) s =
    (emptyIfNull s).Split([| separator |], StringSplitOptions.RemoveEmptyEntries)

let splitByString (separator: string) s =
    (emptyIfNull s).Split([| separator |], StringSplitOptions.RemoveEmptyEntries)

let toOption (s: string) =
    if String.IsNullOrWhiteSpace(s) then None else Some s

let toUtf8Base64 (s: string) =
    let bytes = Encoding.UTF8.GetBytes s
    Convert.ToBase64String bytes

let fromUtf8Base64 (s: string) =
    let bytes = Convert.FromBase64String s
    Encoding.UTF8.GetString bytes

let ofBytesArray (b: byte array) = Encoding.UTF8.GetString b

let toBytesArray (s: string) = Encoding.UTF8.GetBytes s

// -- Functions not supported by Fable ----

#if FABLE_COMPILER
#else

let toAsciiBase64 (s: string) =
    let bytes = Encoding.ASCII.GetBytes s
    Convert.ToBase64String(bytes)

let (|Contains|_|) (p: string) (s: string) = s.Contains(p) |> Option.ofBool

let (|Guid|_|) (s: string) = Guid.TryParse s |> Option.ofPair

#endif

let isInteger (s: string) = Int32.TryParse(s) |> fst

let (|Int|_|) (s: string) = Int32.TryParse s |> Option.ofPair
let (|NotEmpty|_|) s = not (isEmpty s) |> Option.ofBool
let (|NullOrWhiteSpace|_|) s = isEmpty s |> Option.ofBool