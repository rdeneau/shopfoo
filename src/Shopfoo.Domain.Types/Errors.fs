module Shopfoo.Domain.Types.Errors

open System
open Shopfoo.Common

[<AutoOpen>]
module Guards =
  type GuardClauseError =
    { EntityName: string
      ErrorMessage: string }

  type Guard(entityName: string) =
    member val EntityName = entityName

    member inline this.Error errorMessage =
        let guardClauseError: GuardClauseError =
            { EntityName = this.EntityName
              ErrorMessage = errorMessage }

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

    member this.Satisfies(condition, error) = this.Satisfies((), condition, error)

type OperationNotAllowedError = { Operation: string; Reason: string }

type DataRelatedError =
    | DataException of exn
    | DataNotFound of Id: string * Type: string
    | DeserializationIssue of ContentToDeserialize: string * TargetType: string * ExceptionThrown: exn

type Error =
    | Bug of exn
    | DataError of DataRelatedError
    | GuardClause of GuardClauseError
    | OperationNotAllowed of OperationNotAllowedError

[<AutoOpen>]
module Helpers =
    let bug exn = Bug exn |> Error

    let operationNotAllowed operation reason =
        { Operation = operation
          Reason = reason }
        |> Error

    let dataException exn = Error(DataException exn)

    let liftDataRelatedError result = Result.mapError DataError result
    let liftGuardClause result = Result.mapError GuardClause result

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
        | GuardClause _ -> "Guard Clause"
        | OperationNotAllowed _ -> "Operation Not Allowed"

[<RequireQualifiedAccess>]
type ErrorDetailLevel =
    | NoDetail
    | Admin

type ErrorMessage =
    { Message: string
      InnerMessage: string option }

    static member Create(message: string, ?innerMessage) =
        { Message = message
          InnerMessage = innerMessage }

    member this.FullMessage =
        match this.InnerMessage with
        | Some innerMessage -> $"{this.Message} ({innerMessage})"
        | None -> this.Message

[<RequireQualifiedAccess>]
module ErrorMessage =
    let empty =
        ErrorMessage.Create(String.empty)

    let private innerExceptionMessage (exn: exn) =
#if FABLE_COMPILER
        // System.Exception.get_InnerException is not supported by Fable
        None
#else
        exn.InnerException
        |> Option.ofObj
        |> Option.map _.Message
#endif

    let inline ofBug (exn: exn) =
        ErrorMessage.Create $"[Program] Oops, something went wrong: {exn.Message}"

    let inline private ofException (exn: exn) =
        ErrorMessage.Create(exn.Message, ?innerMessage = innerExceptionMessage exn)

    let inline private ofOperationNotAllowed { Operation = op; Reason = reason } =
        ErrorMessage.Create $"Operation [%s{op}] is not allowed. Reason: %s{reason}"

    let ofDataError =
        function
        | DataException exn -> ofException exn
        | DataNotFound(id, (String.NotEmpty as typeName)) -> ErrorMessage.Create $"[%s{typeName}] %s{id} not found"
        | DataNotFound(id, _) -> ErrorMessage.Create $"%s{id} not found"
        | DeserializationIssue(content, targetType, exn) -> ErrorMessage.Create($"Failed to deserialize to %s{targetType}: %s{exn.Message}\nContent: %s{content}", ?innerMessage = innerExceptionMessage exn)

    let ofError =
        function
        | Bug(FirstException exn) -> ofBug exn
        | DataError err -> ofDataError err
        | GuardClause err -> ErrorMessage.Create $"Guard {err.EntityName}: {err.ErrorMessage}"
        | OperationNotAllowed err -> ofOperationNotAllowed err

type ErrorType =
    | Business
    | Technical