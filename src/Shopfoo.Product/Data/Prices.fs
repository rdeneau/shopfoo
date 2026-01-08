[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Prices

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data

module private Fakes =
    let private cleanArchitecture = // ↩
        Prices.Create(SKU.CleanArchitecture, USD, 39.90m)

    let private domainDrivenDesign =
        Prices.Create(SKU.DomainDrivenDesign, EUR, 64.42m, 67.33m)

    let private domainModelingMadeFunctional =
        Prices.Create(SKU.DomainModelingMadeFunctional, EUR, 32.32m, 43.04m)

    let private javaScriptTheGoodParts =
        Prices.Create(SKU.JavaScriptTheGoodParts, EUR, 19.98m, 28.92m)

    let private thePragmaticProgrammer =
        Prices.Create(SKU.ThePragmaticProgrammer, EUR, 32.86m, 49.37m)

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
            return prices
        }

    let savePrices (prices: Prices) =
        async {
            do! Async.Sleep(millisecondsDueTime = 200) // Simulate latency
            return repository |> Dictionary.tryUpdateBy _.SKU prices |> liftDataRelatedError
        }