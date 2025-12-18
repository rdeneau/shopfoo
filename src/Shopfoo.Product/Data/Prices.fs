[<RequireQualifiedAccess>]
module internal Shopfoo.Catalog.Data.Prices

open Shopfoo.Common
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data

module private Fakes =
    let private cleanArchitecture = {
        SKU = SKU.CleanArchitecture
        RetailPrice = Dollars 39.90m
        RecommendedPrice = None
    }

    let private domainDrivenDesign = {
        SKU = SKU.DomainDrivenDesign
        RetailPrice = 64.42m |> Euros
        RecommendedPrice = Some(67.33m |> Euros)
    }

    let private domainModelingMadeFunctional = {
        SKU = SKU.DomainModelingMadeFunctional
        RetailPrice = 32.32m |> Euros
        RecommendedPrice = Some(43.04m |> Euros)
    }

    let private javaScriptTheGoodParts = {
        SKU = SKU.JavaScriptTheGoodParts
        RetailPrice = 19.98m |> Euros
        RecommendedPrice = Some(28.92m |> Euros)
    }

    let private thePragmaticProgrammer = {
        SKU = SKU.ThePragmaticProgrammer
        RetailPrice = 32.86m |> Euros
        RecommendedPrice = Some(49.37m |> Euros)
    }

    let all = [
        cleanArchitecture
        domainDrivenDesign
        domainModelingMadeFunctional
        javaScriptTheGoodParts
        thePragmaticProgrammer
    ]

module Client =
    let repository = Fakes.all |> dictionaryBy _.SKU

    let getPrices sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 100) // Simulate latency
            let prices = repository.Values |> Seq.tryFind (fun x -> x.SKU = sku)
            return Ok prices
        }