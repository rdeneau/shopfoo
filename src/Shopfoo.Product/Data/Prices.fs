[<RequireQualifiedAccess>]
module internal Shopfoo.Catalog.Data.Prices

open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data

let private cleanArchitecture = {
    SKU = SKU.CleanArchitecture
    RetailPrice = 39.90m<euros>
    RecommendedPrice = None
}

let private domainDrivenDesign = {
    SKU = SKU.DomainDrivenDesign
    RetailPrice = 64.42m<euros>
    RecommendedPrice = Some 67.33m<euros>
}

let private domainModelingMadeFunctional = {
    SKU = SKU.DomainModelingMadeFunctional
    RetailPrice = 32.32m<euros>
    RecommendedPrice = Some 43.04m<euros>
}

let private javaScriptTheGoodParts = {
    SKU = SKU.JavaScriptTheGoodParts
    RetailPrice = 19.98m<euros>
    RecommendedPrice = Some 28.92m<euros>
}

let private thePragmaticProgrammer = {
    SKU = SKU.ThePragmaticProgrammer
    RetailPrice = 32.86m<euros>
    RecommendedPrice = Some 49.37m<euros>
}

let private all = [
    cleanArchitecture
    domainDrivenDesign
    domainModelingMadeFunctional
    javaScriptTheGoodParts
    thePragmaticProgrammer
]

let repository = all |> dictionaryBy _.SKU