module Shopfoo.Domain.Types.Errors

open System
open System.Runtime.CompilerServices
open Shopfoo.Common

[<AutoOpen>]
module Validation =
    type Validation<'t, 'e> = Result<'t, 'e list>

    let toValidation (result: Result<'t, 'e>) : Validation<'t, 'e> =
        result |> Result.mapError List.singleton

    type ValidationBuilder() =
        member _.BindReturn(x: Validation<'t, 'e>, f: 't -> 'u) = Result.map f x

        member _.MergeSources(x: Validation<'t, 'e>, y: Validation<'u, 'e>) =
            match (x, y) with
            | Ok v1, Ok v2 -> Ok(v1, v2) // Merge both values in a pair
            | Error e1, Error e2 -> Error(e1 @ e2) // Merge errors into a single list
            | Error e, _
            | _, Error e -> Error e // Short-circuit on single error source

    let validation = ValidationBuilder()

[<AutoOpen>]
module Guards =
    type GuardClauseError = { EntityName: string; ErrorMessage: string }

    type GuardCriteria = {
        MaxLength: int option
        MinLength: int option
        Required: bool
    } with
        static member Create(?maxLength, ?minLength, ?required) : GuardCriteria = {
            MaxLength = maxLength
            MinLength = minLength
            Required = defaultArg required false
        }

        static member None = GuardCriteria.Create()

    type Guard(entityName: string) =
        member val EntityName = entityName

        member inline this.Error errorMessage =
            let guardClauseError: GuardClauseError = { EntityName = this.EntityName; ErrorMessage = errorMessage }

            Error guardClauseError

        member this.IsBoolean(value) =
            match value with
            | String.Bool b -> Ok b
            | _ -> this.Error $"should be a boolean but was {prettyPrintString value}"

        member this.IsDateTime(value) =
            match value with
            | String.DateTime value -> Ok value
            | _ -> this.Error $"should be a DateTime but was {prettyPrintString value}"

        member this.IsDecimal(value) =
            match value with
            | String.Decimal value -> Ok value
            | _ -> this.Error $"should be a decimal but was {prettyPrintString value}"

        member this.IsInteger(value) =
            match value with
            | String.Int value -> Ok value
            | _ -> this.Error $"should be an integer but was {prettyPrintString value}"

        member this.IsNotEmpty(value, ?allowEmpty) =
            match value, allowEmpty with
            | String.NotEmpty, _
            | _, Some true -> Ok value
            | _ -> this.Error $"should not be empty but was {prettyPrintString value}"

        member this.IsNotEmptyGuid(value) =
            match value with
            | _ when value = Guid.Empty -> this.Error "should not be an empty guid"
            | _ -> Ok value

        member inline this.IsPositive(value) =
            match value with
            | IsPositive -> Ok value
            | _ -> this.Error $"should be positive but was {value}"

        member inline this.IsPositiveOrZero(value) =
            match value with
            | IsPositive -> Ok value
            | IsZero -> Ok value
            | _ -> this.Error $"should be positive or zero but was {value}"

        member this.HasExactlyOneElement(sequence) =
            match Seq.tryExactlyOne sequence with
            | Some value -> Ok value
            | None -> this.Error $"should have exactly one element but had {Seq.length sequence}"

        member this.NotSupported(value) =
            this.Error $"{prettyPrintObject value} is not supported"

        member this.Satisfies(value, condition, error) =
            match condition with
            | true -> Ok value
            | false -> this.Error error

        member this.Satisfies(condition, error) = // ↩
            this.Satisfies((), condition, error)

        member this.Satisfies(value, criteria: GuardCriteria) =
            let len = (String.trimWhiteSpace value).Length

            let issuesWithRank = [
                match criteria.Required with
                | true when len = 0 -> 1, "required"
                | _ -> ()

                match criteria.MinLength with
                | Some n when len < n -> 2, $"%i{n} character long min"
                | _ -> ()

                match criteria.MaxLength with
                | Some n when len > n -> 3, $"%i{n} character long max"
                | _ -> ()
            ]

            match issuesWithRank with
            | [] -> Ok value
            | _ ->
                let issues = issuesWithRank |> List.sortBy fst |> List.map snd |> String.concat ", "
                this.Error $"""'%s{value}' should be a string %s{issues}, trailing whitespaces excluded"""

type OperationNotAllowedError = { Operation: string; Reason: string }

type DataRelatedError =
    | DataException of exn
    | DataNotFound of Id: string * Type: string
    | DeserializationIssue of ContentToDeserialize: string * TargetType: string * ExceptionThrown: exn

type Error =
    | Bug of exn
    | DataError of DataRelatedError
    | OperationNotAllowed of OperationNotAllowedError
    | GuardClause of GuardClauseError
    | Validation of GuardClauseError list

[<AutoOpen>]
module Helpers =
    let bug exn = Bug exn |> Error

    let operationNotAllowed operation reason =
        { Operation = operation; Reason = reason } |> Error

    let dataException exn = Error(DataException exn)

    let liftDataRelatedError result = Result.mapError DataError result
    let liftGuardClause result = Result.mapError GuardClause result

    let liftGuardClauses (validation: Validation<'a, GuardClauseError>) =
        validation |> Result.mapError Validation

    let liftOperationNotAllowed result =
        Result.mapError OperationNotAllowed result

    let createAndLiftGuardClause constructor input = constructor input |> liftGuardClause

    let (|FirstExceptions|) (exn: exn) =
        match exn with
#if !FABLE_COMPILER
        | :? AggregateException as exn -> exn.Flatten().InnerExceptions |> List.ofSeq
        | _ when not (isNull exn.InnerException) -> [ exn.InnerException ]
#endif
        | _ -> [ exn ]

    let (|FirstException|) =
        function
        | FirstExceptions(exn :: _)
        | exn -> exn

type GuardExtensions =
    [<Extension>]
    static member LiftError(guardResult: Result<'a, GuardClauseError>) : Result<unit, Error> =
        guardResult |> Result.ignore |> liftGuardClause

    [<Extension>]
    static member ToValidation(guardResult: Result<'a, GuardClauseError>) : Validation<'a, GuardClauseError> = guardResult |> toValidation

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
type TypeName<'a> =
    | FullName
    | Empty

    member this.Value =
        match this with
        | FullName -> typeof<'a>.FullName
        | Empty -> String.empty

[<RequireQualifiedAccess>]
module Result =
    /// <summary>
    /// Similar to <c>Result.ofOption</c>, where the eventual Error case is <c>DataError(DataNotFound(Id = info, Type = typeName.Value))</c>.
    /// </summary>
    let requireSomeData (info: string, typeName: TypeName<'a>) (option: 'a option) : Result<'a, DataRelatedError> =
        match option with
        | Some value -> Ok value
        | None -> Error(DataNotFound(Id = info, Type = typeName.Value))

    /// <summary>
    /// Similar to <c>Result.ofOption</c>, where the eventual Error case is <c>DataError(DataNotFound(Id = info, Type = typeof{'a}.FullName))</c>.
    /// </summary>
    let requireSome info (option: 'a option) =
        requireSomeData (info, TypeName.FullName) option

#endif

module ErrorCategory =
    let inline ofError error =
        match error with
        | Bug _ -> "Bug: Exception"
        | DataError(DataException _) -> "Data Error: Query Issue"
        | DataError(DataNotFound _) -> "Data Error: Not Found"
        | DataError(DeserializationIssue _) -> "Data Error: Deserialization Issue"
        | OperationNotAllowed _ -> "Operation Not Allowed"
        | GuardClause _ -> "Guard Clause"
        | Validation _ -> "Validation"

[<RequireQualifiedAccess>]
type ErrorDetailLevel =
    | NoDetail
    | Admin

type ErrorMessage = {
    Message: string
    InnerMessage: string option
} with
    static member Create(message: string, ?innerMessage) = { Message = message; InnerMessage = innerMessage }

    static member Combine(errorMessages) = {
        Message = errorMessages |> List.map _.Message |> String.concat "\n"
        InnerMessage =
            match errorMessages |> List.choose _.InnerMessage with
            | [] -> None
            | xs -> Some(xs |> String.concat "\n")
    }

    member this.FullMessage =
        match this.InnerMessage with
        | Some innerMessage -> $"{this.Message} ({innerMessage})"
        | None -> this.Message

[<RequireQualifiedAccess>]
module ErrorMessage =
    let empty = ErrorMessage.Create(String.empty)

    let private innerExceptionMessage (exn: exn) =
#if FABLE_COMPILER
        // System.Exception.get_InnerException is not supported by Fable
        None
#else
        exn.InnerException |> Option.ofObj |> Option.map _.Message
#endif

    let inline ofBug (exn: exn) =
        ErrorMessage.Create $"[Program] Oops, something went wrong: {exn.Message}"

    let inline private ofException (exn: exn) =
        ErrorMessage.Create(exn.Message, ?innerMessage = innerExceptionMessage exn)

    let inline private ofGuardClause { EntityName = name; ErrorMessage = message } =
        ErrorMessage.Create $"Guard %s{name}: %s{message}"

    let inline private ofOperationNotAllowed { Operation = op; Reason = reason } =
        ErrorMessage.Create $"Operation [%s{op}] is not allowed. Reason: %s{reason}"

    let ofDataError =
        function
        | DataException exn -> ofException exn
        | DataNotFound(id, (String.NotEmpty as typeName)) -> ErrorMessage.Create $"[%s{typeName}] %s{id} not found"
        | DataNotFound(id, _) -> ErrorMessage.Create $"%s{id} not found"
        | DeserializationIssue(content, targetType, exn) ->
            ErrorMessage.Create(
                $"Failed to deserialize to %s{targetType}: %s{exn.Message}\nContent: %s{content}",
                ?innerMessage = innerExceptionMessage exn
            )

    let ofError =
        function
        | Bug(FirstException exn) -> ofBug exn
        | DataError err -> ofDataError err
        | OperationNotAllowed err -> ofOperationNotAllowed err
        | GuardClause err -> ofGuardClause err
        | Validation errors -> errors |> List.map ofGuardClause |> ErrorMessage.Combine

type ErrorType =
    | Business
    | Technical