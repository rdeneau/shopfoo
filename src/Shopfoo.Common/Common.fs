[<AutoOpen>]
module Shopfoo.Common.Common

open System
open System.Collections.Generic
open System.Globalization

/// <summary>
/// Create a <c>Dictionary</c> from the given <paramref name="items"/>,
/// using <paramref name="getKey"/> to determine the key for each item.
/// </summary>
/// <param name="getKey">Determine the key for each item.</param>
/// <param name="items">Items to put in the dictionary</param>
let dictionaryBy getKey items =
    dict [ for item in items -> getKey item, item ] |> Dictionary

/// <summary>
/// Abstraction of <c>CultureInfo.InvariantCulture</c> compatible with Fable.
/// </summary>
[<RequireQualifiedAccess>]
type InvariantCulture =
#if FABLE_COMPILER
    static member ListSeparator = ","

    static member Format(value) = value.ToString()

    static member ParseDateOnly(s: string) = DateOnly.FromDateTime(DateTime.Parse s)
    static member ParseDateTime(s: string) = DateTime.Parse(s)
    static member ParseDecimal(s: string) = Decimal.Parse(s)

    static member TryParseDateOnly(s: string) =
        match DateTime.TryParse(s) with
        | true, dateTime -> true, DateOnly.FromDateTime dateTime
        | false, _ -> false, DateOnly.MinValue

    static member TryParseDateTime(s: string) = DateTime.TryParse(s)
    static member TryParseDecimal(s: string) = Decimal.TryParse(s)
#else
    static member ListSeparator = CultureInfo.InvariantCulture.TextInfo.ListSeparator

    static member Format(value: 't) =
        String.Format(CultureInfo.InvariantCulture, "{0}", value)

    static member ParseDateOnly(s: string) =
        DateOnly.Parse(s, CultureInfo.InvariantCulture)

    static member ParseDateTime(s: string) =
        DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.None)

    static member ParseDecimal(s: string) =
        Decimal.Parse(s, CultureInfo.InvariantCulture)

    static member TryParseDateOnly(s: string) =
        DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None)

    static member TryParseDateTime(s: string) =
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None)

    static member TryParseDecimal(s: string) =
        Decimal.TryParse(s, CultureInfo.InvariantCulture)
#endif

[<RequireQualifiedAccess>]
module Option =
    /// <summary>
    /// Wrap the <paramref name="value"/> in an <see cref="Option"/> if it's a <paramref name="success"/>.
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
    /// <param name="success">Flag controlling if the returned <see cref="Option"/> is <c>Some</c>.</param>
    /// <remarks>
    /// 💡 Useful to build partial active pattern before F# 9: see the example.
    /// </remarks>
    /// <example>
    /// <code>let (|Satisfy|_|) predicate x = predicate x |> Option.ofBool</code>
    /// </example>
    let ofBool success = ofPair (success, ())

    /// <summary>
    /// Wrap the given string <paramref name="s"/> in an <see cref="Option"/>
    /// </summary>
    /// <param name="s"></param>
    let ofNonNullOrWhitespace s =
        match String.IsNullOrWhiteSpace s with
        | false -> Some s
        | true -> None

[<RequireQualifiedAccess>]
module Result =
    let bindError fError (xR: Result<'x, 'e1>) : Result<'x, 'e2> =
        match xR with
        | Ok x -> Ok x
        | Error err -> fError err

    let inline ignore xR = Result.map ignore xR

    let mapOption (f: 'a -> Result<'b, 'e>) (opt: 'a option) : Result<'b option, 'e> =
        match opt with
        | None -> Ok None
        | Some a ->
            match f a with
            | Ok value -> Ok(Some value)
            | Error errorValue -> Error errorValue

    let tryGetError (xR: Result<'x, 'e>) =
        match xR with
        | Ok _ -> None
        | Error e -> Some e

[<AutoOpen>]
module ActivePatterns =
    let (|Between|_|) min max value =
        Option.ofBool (value >= min && value <= max)

    let (|In|_|) values value =
        Option.ofBool (Seq.contains value values)

    let inline (|IsNegative|_|) number =
        Option.ofBool (number < LanguagePrimitives.GenericZero)

    let inline (|IsPositive|_|) number =
        Option.ofBool (number > LanguagePrimitives.GenericZero)

    let inline (|IsZero|_|) number =
        Option.ofBool (number = LanguagePrimitives.GenericZero)

    let (|Null|NotNull|) value =
        match isNull (box value) with
        | true -> Null
        | false -> NotNull

    /// <summary>
    /// Active pattern that can be used to replace a guard in a <c>match</c> expression. <br/>
    /// Benefit: composable — Drawback: can need a lambda.
    /// </summary>
    /// <param name="predicate">Function to check the value</param>
    /// <param name="value">Value to check</param>
    /// <example>
    /// Before:
    /// <code>
    /// match opt with
    /// | Some s when String.IsNullOrWhitespace(s) -> xxx
    /// | None | Some "N/A" -> xxx // ⚠️ Due to the guard, xxx can not be mutualized
    /// | _ -> yyy
    /// </code>
    ///
    /// After:
    /// <code>
    /// match opt with
    /// | None | Some (Satisfy String.IsNullOrWhitespace | "N/A") -> xxx
    /// | _ -> yyy
    /// </code>
    /// </example>
    let inline (|Satisfy|_|) ([<InlineIfLambda>] predicate) value = predicate value |> Option.ofBool

[<RequireQualifiedAccess>]
module Nullable =
    let (|Value|Null|) (nullable: Nullable<'t>) =
        match Option.ofNullable nullable with
        | Some value -> Value value
        | None -> Null

[<AutoOpen>]
module DateOnlyExtensions =
    type DateOnly with
        member this.ToDateTime() = // ↩
            this.ToDateTime(TimeOnly.MinValue)

        static member Today = DateOnly.FromDateTime(DateTime.Today)

[<RequireQualifiedAccess>]
module DateOnly =
    let Default = DateOnly(1900, 1, 1)

    let toInvariantString (d: DateOnly) = // ↩
        InvariantCulture.Format d

    let tryParse (s: String) =
        InvariantCulture.TryParseDateOnly s |> Option.ofPair

[<RequireQualifiedAccess>]
module TimeOnly =
    let (|HoursOnly|HoursMinutes|HoursMinutesSeconds|) (timeOnly: TimeOnly) =
        match timeOnly.Minute, timeOnly.Second with
        | 0, 0 -> HoursOnly
        | _, 0 -> HoursMinutes
        | _, _ -> HoursMinutesSeconds

#if !FABLE_COMPILER // System.TimeOnly.ToString is not supported by Fable
    let formatTimeInvariant (timeOnly: TimeOnly) =
        let format =
            match timeOnly with
            | HoursOnly -> (************) "hh" (*******) // E.g. 9AM,     3PM
            | HoursMinutes -> (*********) "H:mm" (*****) // E.g. 9:05,    15:30
            | HoursMinutesSeconds -> (**) "H:mm:ss" (**) // E.g. 9:00:30, 15:15:15

        timeOnly.ToString(format, CultureInfo.InvariantCulture)
#endif

[<RequireQualifiedAccess>]
module Enum =
    let inline values<'enum when 'enum :> Enum and 'enum: comparison> =
        Enum.GetValues(typeof<'enum>) :?> 'enum array |> Set.ofArray

    let inline flags<'enum when 'enum :> Enum and 'enum: comparison> (enumValue: 'enum) =
        values<'enum> |> Set.filter enumValue.HasFlag

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Environment =
    open System.IO

    module Operators =
        let (</>) x y = Path.Combine(x, y)

#endif

[<AutoOpen>]
module Exception =
    open System.Reflection

    let inline reraisePreserveStackTrace (e: Exception) =
        let remoteStackTraceString =
            typeof<exn>.GetField("_remoteStackTraceString", BindingFlags.Instance ||| BindingFlags.NonPublic)

        remoteStackTraceString.SetValue(e, e.StackTrace + Environment.NewLine)
        raise e

[<RequireQualifiedAccess>]
module Regex =
    open System.Text.RegularExpressions

    let (|Match|_|) pattern value =
        let m = Regex.Match(value, pattern)

        if not m.Success || m.Groups.Count < 1 then
            None
        else
            Some [
                for i = 1 to m.Groups.Count - 1 do // Start index at 1 (not 0) to ignore the "root" match
                    m.Groups[i].Value
            ]

    let (|Matches|_|) pattern value =
        [
            for m in Regex.Matches(value, pattern) do
                if m.Success && m.Groups.Count > 0 then
                    [
                        for i = 1 to m.Groups.Count - 1 do // Start index at 1 (not 0) to ignore the "root" match
                            m.Groups[i].Value
                    ]
        ]
        |> Some
        |> Option.filter (not << List.isEmpty)

[<AutoOpen>]
module PrettyPrint =
    let prettyPrintString s =
        match s with
        | null -> "null"
        | _ -> $"‟%s{s}”"

    let prettyPrintObject (o: obj) =
        match o with
        | null -> "null"
        | :? bool
        | :? int
        | :? float
        | :? decimal -> InvariantCulture.Format(o)
        | :? string as s -> prettyPrintString s
        | _ -> $"%A{o}"

    let prettyPrintOption option =
        match option with
        | None -> "None"
        | Some o -> prettyPrintObject o

    let prettyConcat texts =
        String.concat InvariantCulture.ListSeparator texts

#if !FABLE_COMPILER

module Type =
    open System.Reflection

    let allLiteralsOfType<'t> (parentType: Type) =
        parentType.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun x -> x.IsLiteral && not x.IsInitOnly && x.FieldType = typeof<'t>)
        |> Seq.map (fun x -> {| Name = x.Name; Value = x.GetRawConstantValue() :?> 't |})

#endif