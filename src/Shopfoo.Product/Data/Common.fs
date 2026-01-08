[<AutoOpen>]
module internal Shopfoo.Product.Data.Common

open System
open Shopfoo.Domain.Types

[<RequireQualifiedAccess>]
module SKU =
    let CleanArchitecture = SKU "9780134494166"
    let DomainDrivenDesign = SKU "9780321125217"
    let DomainModelingMadeFunctional = SKU "9781680502541"
    let JavaScriptTheGoodParts = SKU "9780596517748"
    let ThePragmaticProgrammer = SKU "9780135957059"
    let none = SKU null

let daysAgo (days: int) =
    DateOnly.FromDateTime(DateTime.Now.AddDays(-days))