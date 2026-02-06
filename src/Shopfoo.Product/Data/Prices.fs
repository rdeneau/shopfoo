module Shopfoo.Product.Data.Prices

open System.Collections.Generic
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Sales
open Shopfoo.Product.Data
open Shopfoo.Product.Data.FakeStore

type PricesRepository = Dictionary<SKU, Prices>

type internal PricesPipeline(repository: PricesRepository, fakeStorePipeline: FakeStorePipeline) =
    member _.GetPrices(sku: SKU) : Async<Prices option> =
        async {
            match sku.Type with
            | SKUType.ISBN _ ->
                do! Fake.latencyInMilliseconds 100
                let prices = repository.Values |> Seq.tryFind (fun x -> x.SKU = sku)
                return prices
            | SKUType.FSID fsid -> return! fakeStorePipeline.GetPrice fsid
            | SKUType.OLID _
            | SKUType.Unknown -> return None
        }

    member _.SavePrices(prices: Prices) : Async<Result<unit, Error>> =
        async {
            do! Fake.latencyInMilliseconds 250
            return repository |> Dictionary.tryUpdateBy _.SKU prices |> liftDataRelatedError
        }

    member _.AddPrices(prices: Prices) : Async<Result<unit, 'a>> =
        async {
            do! Fake.latencyInMilliseconds 200
            repository.Add(prices.SKU, prices)
            return Ok()
        }

module private Fakes =
    let private cleanArchitecture = Prices.Create(ISBN.CleanArchitecture, USD, 39.90m)
    let private cleanCode = Prices.Create(ISBN.CleanCode, EUR, 46.46m, 54.20m)
    let private fsharpInActions = Prices.Create(ISBN.FsharpInActions, EUR, 54.29m)
    let private codeThatFitsInYourHead = Prices.Create(ISBN.CodeThatFitsInYourHead, EUR, 33.78m, 36.10m)
    let private dependencyInjection = Prices.Create(ISBN.DependencyInjection, EUR, 52.08m, 54.20m)
    let private domainDrivenDesign = Prices.Create(ISBN.DomainDrivenDesign, EUR, 64.42m, 67.33m)
    let private domainModelingMadeFunctional = Prices.Create(ISBN.DomainModelingMadeFunctional, EUR, 32.32m, 43.04m)
    let private howJavaScriptWorks = Prices.Create(ISBN.HowJavaScriptWorks, EUR, 22.72m)
    let private javaScriptTheGoodParts = Prices.Create(ISBN.JavaScriptTheGoodParts, EUR, 19.98m, 28.92m)
    let private thePragmaticProgrammer = Prices.Create(ISBN.ThePragmaticProgrammer, EUR, 32.86m, 49.37m)
    let private unitTesting = Prices.Create(ISBN.UnitTesting, EUR, 36.44m, 45.17m)

    let allPrices = [
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

[<RequireQualifiedAccess>]
module internal PricesRepository =
    let instance: PricesRepository = Fakes.allPrices |> Dictionary.ofListBy _.SKU