[<AutoOpen>]
module internal Shopfoo.Product.Data.Common

open System
open Shopfoo.Domain.Types

[<RequireQualifiedAccess>]
module SKU =
    let CleanArchitecture = SKU "0134494164"
    let DomainDrivenDesign = SKU "0321125215"
    let DomainModelingMadeFunctional = SKU "9781680502541"
    let JavaScriptTheGoodParts = SKU "9780596107130"
    let ThePragmaticProgrammer = SKU "0135957052"
    let none = SKU null

let daysAgo (days: int) =
    DateOnly.FromDateTime(DateTime.Now.AddDays(-days))
