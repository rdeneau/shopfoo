[<AutoOpen>]
module Shopfoo.Product.Data.Common

open System
open System.Collections.Generic
open System.Linq
open Shopfoo.Common
open Shopfoo.Domain.Types

[<RequireQualifiedAccess>]
module internal Fake =
    let latencyInMilliseconds ms = Async.Sleep(millisecondsDueTime = ms)

[<RequireQualifiedAccess>]
module ISBN =
    let CleanArchitecture = ISBN "9780134494166"
    let CleanCode = ISBN "9780132350884"
    let CodeThatFitsInYourHead = ISBN "9780137464326"
    let DependencyInjection = ISBN "9781617294730"
    let DomainDrivenDesign = ISBN "9780321125217"
    let DomainDrivenDesignReference = ISBN "9781457501197"
    let DomainModelingMadeFunctional = ISBN "9781680502541"
    let FsharpInActions = ISBN "9781638355212"
    let HowJavaScriptWorks = ISBN "9781949815009"
    let JavaScriptTheGoodParts = ISBN "9780596517748"
    let Refactoring = ISBN "9780134757599"
    let ThePragmaticProgrammer = ISBN "9780135957059"
    let UnitTesting = ISBN "9781617296277"

let daysAgo (days: int) = DateOnly.FromDateTime(DateTime.Now.AddDays(-days))

type internal FakeRepository<'k, 'v when 'k: comparison>(data: 'v seq, getKey: 'v -> 'k) =
    let repository: Dictionary<'k, ResizeArray<'v>> =
        data
        |> Seq.groupBy getKey
        |> Seq.map (fun (key, values) -> key, ResizeArray values)
        |> _.ToDictionary(fst, snd)

    member _.Add(value: 'v) : unit =
        let key = getKey value
        match repository.TryGetValue(key) with
        | true, events -> events.Add(value)
        | false, _ -> repository.Add(key, ResizeArray [ value ])

    member _.Get(key: 'k) : 'v list option =
        repository.TryGetValue(key) // ↩
        |> Option.ofPair
        |> Option.map List.ofSeq