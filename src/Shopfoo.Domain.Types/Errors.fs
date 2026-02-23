module Shopfoo.Domain.Types.Errors

open System
open System.Runtime.CompilerServices
open Shopfoo.Common

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

[<Interface>]
type IBusinessError =
    abstract member Code: string
    abstract member Message: string

[<AutoOpen>]
module Validation =
    type Validation<'t, 'e> = Result<'t, 'e list>

    let toValidation (result: Result<'t, 'e>) : Validation<'t, 'e> = // ↩
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
        PropertyName: string
        MaxLength: int option
        MinLength: int option
        Required: bool
    } with
        static member Create(name, ?maxLength, ?minLength, ?required) : GuardCriteria = {
            PropertyName = name
            MaxLength = maxLength
            MinLength = minLength
            Required = defaultArg required false
        }

        static member None = GuardCriteria.Create("")

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

        member this.NotSupported(value) = // ↩
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
                this.Error $"""%s{criteria.PropertyName} '%s{value}' does not satisfy the criteria: %s{issues}, trailing whitespaces excluded"""

[<AutoOpen>]
module Http =
    [<RequireQualifiedAccess>]
    type HttpVerb =
        | Get
        | Post
        | Put
        | Patch
        | Delete

    type HttpStatus = {
        Code: int
        Label: string
        Content: string option
        Request: string option
        Verb: HttpVerb
        Uri: Uri option
    } with
        member this.Explain() =
            let verb = this.Verb.ToString() |> String.toUpper

            let uri =
                match this.Uri with
                | Some uri -> $"\n%s{verb} %s{uri.ToString()}"
                | _ -> String.empty

            let request =
                match this.Request with
                | Some(String.NotEmpty as request) -> $"\nRequest: %s{request}"
                | _ -> String.empty

            let response =
                match this.Content with
                | Some(String.NotEmpty as content) -> $"\nResponse: %s{content}"
                | _ -> String.empty

            $"HTTP Status Code %i{this.Code} (%s{this.Label})%s{uri}%s{request}%s{response}"

        static member Create(code: int, label: string, ?content) = {
            Code = code
            Label = label
            Content = content
            Request = None
            Verb = HttpVerb.Get
            Uri = None
        }

#if !FABLE_COMPILER

    type HttpStatus with
        /// <remarks>
        /// This method is not available in Fable. Use the <c>HttpStatus.Create</c> method instead.
        /// </remarks>
        static member FromHttpStatusCode(httpStatusCode: System.Net.HttpStatusCode, ?content, ?request, ?verb, ?uri) = {
            Code = int httpStatusCode
            Label = httpStatusCode.ToString()
            Content = content
            Request = request
            Verb = defaultArg verb HttpVerb.Get
            Uri = uri
        }

#endif

    type HttpApiName =
        | HttpApiName of string
        member this.Code = let (HttpApiName code) = this in code

    [<RequireQualifiedAccess>]
    module HttpApiName =
        let FakeStore = HttpApiName "FakeStore"
        let OpenLibrary = HttpApiName "OpenLibrary"

type DataRelatedError =
    | DataException of exn
    | DataNotFound of Id: string * Type: string
    | DuplicateKey of Id: string * Type: string
    | DeserializationIssue of ContentToDeserialize: string * TargetType: string * ExceptionThrown: exn
    | HttpApiError of apiName: HttpApiName * status: HttpStatus

type OperationNotAllowedError = { Operation: string; Reason: string }

type WorkflowError =
    | WorkflowCancelled of step: string
    | WorkflowUndoError of reason: string

type Error =
    | BusinessError of IBusinessError
    | Bug of exn
    | DataError of DataRelatedError
    | OperationNotAllowed of OperationNotAllowedError
    | GuardClause of GuardClauseError
    | Validation of GuardClauseError list
    | WorkflowError of WorkflowError
    | Errors of Error list

[<AutoOpen>]
module Helpers =
    let bug exn = Error(Bug exn)
    let operationNotAllowed operation reason = Error { Operation = operation; Reason = reason }
    let dataException exn = Error(DataException exn)

    let liftDataRelatedError result = Result.mapError DataError result
    let liftOperationNotAllowed result = Result.mapError OperationNotAllowed result
    let liftGuardClause guardClause = Result.mapError GuardClause guardClause
    let liftValidation (validation: Validation<'a, GuardClauseError>) = validation |> Result.mapError Validation

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
    static member LiftError(guardResult: Result<'a, GuardClauseError>) : Result<unit, Error> = // ↩
        guardResult |> Result.ignore |> liftGuardClause

    [<Extension>]
    static member ToValidation(guardResult: Result<'a, GuardClauseError>) : Validation<'a, GuardClauseError> = // ↩
        guardResult |> toValidation

module ErrorCategory =
    let rec ofError error =
        match error with
        | BusinessError err -> $"Business Error: %s{err.Code}"
        | Bug _ -> "Bug: Exception"
        | DataError(DataException _) -> "Data Error: Query Issue"
        | DataError(DataNotFound _) -> "Data Error: Not Found"
        | DataError(DeserializationIssue _) -> "Data Error: Deserialization Issue"
        | DataError(DuplicateKey _) -> "Data Error: Duplicate Key"
        | DataError(HttpApiError _) -> "Data Error: HTTP API Issue"
        | OperationNotAllowed _ -> "Operation Not Allowed"
        | GuardClause _ -> "Guard Clause"
        | Validation _ -> "Validation"
        | WorkflowError(WorkflowCancelled _) -> "Workflow Cancelled"
        | WorkflowError(WorkflowUndoError _) -> "Workflow Undo Error"
        | Errors errors ->
            let errors = [| for err in errors -> ofError err |]
            let errorCategories = errors |> String.concat " | "
            $"%i{errors.Length} Errors: %s{errorCategories}"

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

    let inline ofBug (exn: exn) = ErrorMessage.Create $"[Program] Oops, something went wrong: {exn.Message}"
    let inline private ofException (exn: exn) = ErrorMessage.Create(exn.Message, ?innerMessage = innerExceptionMessage exn)
    let inline private ofGuardClause { EntityName = name; ErrorMessage = message } = ErrorMessage.Create $"Guard %s{name}: %s{message}"

    let inline private ofOperationNotAllowed { Operation = op; Reason = reason } =
        ErrorMessage.Create $"Operation [%s{op}] is not allowed. Reason: %s{reason}"

    let ofDataError =
        function
        | DataException exn -> ofException exn
        | DataNotFound(id, (String.NotEmpty as typeName)) -> ErrorMessage.Create $"[%s{typeName}] %s{id} not found"
        | DataNotFound(id, _) -> ErrorMessage.Create $"%s{id} not found"
        | DuplicateKey(id, (String.NotEmpty as typeName)) -> ErrorMessage.Create $"[%s{typeName}] %s{id} already exists"
        | DuplicateKey(id, _) -> ErrorMessage.Create $"%s{id} already exists"
        | DeserializationIssue(content, targetType, exn) ->
            ErrorMessage.Create(
                $"Failed to deserialize to %s{targetType}: %s{exn.Message}\nContent: %s{content}",
                ?innerMessage = innerExceptionMessage exn
            )
        | HttpApiError(apiName, status) ->
            ErrorMessage.Create( // ↩
                $"Call to %s{apiName.Code} API failed with %s{status.Explain()}",
                ?innerMessage = status.Content
            )

    let ofWorkflowError =
        function
        | WorkflowUndoError reason -> ErrorMessage.Create $"Workflow undo failed: %s{reason}"
        | WorkflowCancelled stepName -> ErrorMessage.Create $"Workflow cancelled at step %s{stepName}"

    let rec ofError =
        function
        | BusinessError err -> ErrorMessage.Create $"Business Error %s{err.Code}: %s{err.Message}"
        | Bug(FirstException exn) -> ofBug exn
        | DataError err -> ofDataError err
        | OperationNotAllowed err -> ofOperationNotAllowed err
        | GuardClause err -> ofGuardClause err
        | Validation errors -> errors |> List.map ofGuardClause |> ErrorMessage.Combine
        | WorkflowError err -> ofWorkflowError err
        | Errors errors -> errors |> List.map ofError |> ErrorMessage.Combine

[<RequireQualifiedAccess>]
type TypeName<'a> =
    | FullName
    | Empty
    | Custom of typeName: string

    member inline this.Value =
        match this with
        | FullName -> typeof<'a>.FullName
        | Empty -> String.empty
        | Custom typeName -> typeName

type Res<'ret> = Result<'ret, Error>

[<RequireQualifiedAccess>]
module Result =
    /// <summary>
    /// Similar to <c>Result.ofOption</c>, where the eventual Error case is <c>DataError(DataNotFound(Id = info, Type = typeName.Value))</c>.
    /// </summary>
    let inline requireSomeData (info: string, typeName: TypeName<'a>) (value: 'a option) : Result<'a, DataRelatedError> =
        match value with
        | Some value -> Ok value
        | None -> Error(DataNotFound(Id = info, Type = typeName.Value))

    /// <summary>
    /// Similar to <c>Result.ofOption</c>, where the eventual Error case is <c>DataError(DataNotFound(Id = info, Type = typeof{'a}.FullName))</c>.
    /// </summary>
    let inline requireSome info (value: 'a option) = requireSomeData (info, TypeName.FullName) value

    // Combine the two given results: both Ok values into a tuple, both Error into the Errors case, a single Error discards the Ok value.
    let zip (resA: Res<'a>) (resB: Res<'b>) : Res<'a * 'b> =
        match resA, resB with
        | Ok v1, Ok v2 -> Ok(v1, v2)
        | Error e1, Error e2 -> Error(Errors [ e1; e2 ])
        | Error e, _ -> Error e
        | _, Error e -> Error e