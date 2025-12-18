module Shopfoo.Effects.Metrics

open System.Diagnostics

type FailureOrigin =
    | Client
    | Server
    | Unknown

    override this.ToString() =
        match this with
        | Client -> "client"
        | Server -> "server"
        | Unknown -> "unknown"

type MetricsStatus =
    | Informed
    | Succeeded
    | Redirected
    | Failed of FailureOrigin option

    override this.ToString() =
        match this with
        | Informed -> "informed"
        | Succeeded -> "succeeded"
        | Redirected -> "redirected"
        | Failed None -> "failed"
        | Failed(Some origin) -> $"{origin}failed"

    static member ofOption(_: _ option) = Succeeded

    static member ofOptionExpected(response: _ option) =
        match response with
        | Some _ -> Succeeded
        | None -> Failed None

    static member ofResult(response: Result<_, _>) =
        match response with
        | Ok _ -> Succeeded
        | Error _ -> Failed None

[<Interface>]
type IMetricsSender =
    abstract SendTimeAsync: funcName: string * MetricsStatus * Stopwatch -> Async<unit>