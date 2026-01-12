[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Prices

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data

module private Fakes =
    let private cleanArchitecture = Prices.Create(ISBN.CleanArchitecture, USD, 39.90m)
    let private cleanCode = Prices.Create(ISBN.CleanCode, EUR, 46.46m, 54.20m)
    let private fsharpInActions = Prices.Create(ISBN.FsharpInActions, EUR, 54.29m)

    let private codeThatFitsInYourHead =
        Prices.Create(ISBN.CodeThatFitsInYourHead, EUR, 33.78m, 36.10m)

    let private dependencyInjection =
        Prices.Create(ISBN.DependencyInjection, EUR, 52.08m, 54.20m)

    let private domainDrivenDesign =
        Prices.Create(ISBN.DomainDrivenDesign, EUR, 64.42m, 67.33m)

    let private domainModelingMadeFunctional =
        Prices.Create(ISBN.DomainModelingMadeFunctional, EUR, 32.32m, 43.04m)

    let private howJavaScriptWorks = Prices.Create(ISBN.HowJavaScriptWorks, EUR, 22.72m)

    let private javaScriptTheGoodParts =
        Prices.Create(ISBN.JavaScriptTheGoodParts, EUR, 19.98m, 28.92m)

    let private thePragmaticProgrammer =
        Prices.Create(ISBN.ThePragmaticProgrammer, EUR, 32.86m, 49.37m)

    let private unitTesting = Prices.Create(ISBN.UnitTesting, EUR, 36.44m, 45.17m)

    let all = [
        cleanArchitecture
        cleanCode
        codeThatFitsInYourHead
        dependencyInjection
        domainDrivenDesign
        domainModelingMadeFunctional
        fsharpInActions
        howJavaScriptWorks
        javaScriptTheGoodParts
        thePragmaticProgrammer
        unitTesting
    ]

module Pipeline =
    let repository = Fakes.all |> Dictionary.ofListBy _.SKU

    let getPrices (sku: SKU) =
        async {
            match sku.Type with
            | SKUType.ISBN _ ->
                do! Async.Sleep(millisecondsDueTime = 100) // Simulate latency
                let prices = repository.Values |> Seq.tryFind (fun x -> x.SKU = sku)
                return prices
            | SKUType.FSID fsid -> return! FakeStore.Pipeline.getPrice fsid
            | SKUType.Unknown -> return None
        }

    let savePrices (prices: Prices) =
        async {
            do! Async.Sleep(millisecondsDueTime = 200) // Simulate latency
            return repository |> Dictionary.tryUpdateBy _.SKU prices |> liftDataRelatedError
        }