[<RequireQualifiedAccess>]
module internal Shopfoo.Catalog.Data.Prices

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data

module private Fakes =
    let private cleanArchitecture = {
        SKU = SKU.CleanArchitecture
        RetailPrice = Dollars 39.90m
        ListPrice = None
    }

    let private domainDrivenDesign = {
        SKU = SKU.DomainDrivenDesign
        RetailPrice = 64.42m |> Euros
        ListPrice = Some(67.33m |> Euros)
    }

    let private domainModelingMadeFunctional = {
        SKU = SKU.DomainModelingMadeFunctional
        RetailPrice = 32.32m |> Euros
        ListPrice = Some(43.04m |> Euros)
    }

    let private javaScriptTheGoodParts = {
        SKU = SKU.JavaScriptTheGoodParts
        RetailPrice = 19.98m |> Euros
        ListPrice = Some(28.92m |> Euros)
    }

    let private thePragmaticProgrammer = {
        SKU = SKU.ThePragmaticProgrammer
        RetailPrice = 32.86m |> Euros
        ListPrice = Some(49.37m |> Euros)
    }

    let all = [
        cleanArchitecture
        domainDrivenDesign
        domainModelingMadeFunctional
        javaScriptTheGoodParts
        thePragmaticProgrammer
    ]

module Client =
    let repository = Fakes.all |> Dictionary.ofListBy _.SKU

    let getPrices sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 100) // Simulate latency
            let prices = repository.Values |> Seq.tryFind (fun x -> x.SKU = sku)
            return Ok prices
        }

    let savePrices (prices: Prices) =
        async {
            do! Async.Sleep(millisecondsDueTime = 200) // Simulate latency
            return repository |> Dictionary.tryUpdateBy _.SKU prices |> liftDataRelatedError
        }