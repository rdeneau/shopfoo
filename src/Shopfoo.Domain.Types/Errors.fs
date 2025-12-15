module Shopfoo.Domain.Types.Errors

open System
open Shopfoo.Common

type ValidationError = { FieldPath: string; Message: string }

module ValidationError =
    let ofField fieldPath errorMessage = { FieldPath = fieldPath; Message = errorMessage }

    let relativeTo parentError validationError = {
        FieldPath = $"%s{parentError.FieldPath}.{validationError.FieldPath}"
        Message = $"{validationError.Message}. %s{parentError.Message}."
    }

type OperationNotAllowedError = { Operation: string; Reason: string }

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
        let verb = this.Verb.ToString().ToUpperInvariant()

        let uri =
            match this.Uri with
            | Some uri -> $"\n%s{verb} %s{uri.ToString()}"
            | _ -> String.Empty

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

type DataRelatedError =
    | DataNotFound of Id: string * Type: string
    | DeserializationIssue of ContentToDeserialize: string * TargetType: string * ExceptionThrown: exn
    | HttpApiError of apiName: string * status: HttpStatus
    | UpdateError of string

type Error =
    | ValidationError of ValidationError
    | OperationNotAllowed of OperationNotAllowedError
    | DataError of DataRelatedError
    | Bug of exn

let (|MailNotFound|_|) =
    function
    | DataError(DataNotFound("Mail", _)) -> Some()
    | _ -> None

let validationError fieldPath message =
    { FieldPath = fieldPath; Message = message } |> Error

let bug exn = Bug exn |> Error

let operationNotAllowed operation reason =
    { Operation = operation; Reason = reason } |> Error

let expectValidationError result = Result.mapError ValidationError result
let expectDataRelatedError result = Result.mapError DataError result

let createOrValidationError constructor input =
    constructor input |> expectValidationError

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

[<RequireQualifiedAccess>]
type ErrorDetailLevel =
    | NoDetail
    | Admin

module ErrorMessage =
    let inline private dataErrorMessage dataError =
        match dataError with
        | UpdateError error -> error
        | DataNotFound(id, dataType) -> $"[%s{dataType}] %s{id} not found"
        | DeserializationIssue(content, targetType, exn) -> $"Failed to deserialize to %s{targetType}: %s{exn.Message}\nContent: %s{content}"
        | HttpApiError(apiName, status) -> $"Call to %s{apiName} API failed with %s{status.Explain()}"

    let inline private validationErrorMessage { FieldPath = path; Message = message } =
        $"Field [%s{path}] is invalid. Reason: %s{message}"

    let inline private operationNotAllowedMessage { Operation = op; Reason = reason } =
        $"Operation [%s{op}] is not allowed. Reason: %s{reason}"

    let inline private bugMessage (exn: exn) =
        $"[Program] Oops, something went wrong: {exn.Message}"

    let inline private errorCase error =
        match error with
        | Bug _ -> ""
        | DataError(DataNotFound _) -> "[DataError:DataNotFound] "
        | DataError(DeserializationIssue _) -> "[DataError:DeserializationIssue]"
        | DataError(HttpApiError _) -> "[DataError:HttpApiError]"
        | DataError(UpdateError _) -> "[DataError:UpdateError] "
        | ValidationError _ -> "[ValidationError] "
        | OperationNotAllowed _ -> "[OperationNotAllowed] "

    let inline ofError error level =
        let errorMessage =
            match error with
            | Bug(FirstException exn) -> bugMessage exn
            | DataError err -> dataErrorMessage err
            | ValidationError err -> validationErrorMessage err
            | OperationNotAllowed err -> operationNotAllowedMessage err

        match level with
        | ErrorDetailLevel.NoDetail -> errorMessage
        | ErrorDetailLevel.Admin -> $"%s{errorCase error}%s{errorMessage}"

type ErrorType =
    | Business
    | Technical

type ValidationResult<'a> = Result<'a, ValidationError>