[<RequireQualifiedAccess>]
module internal Shopfoo.Catalog.Data.Catalog

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Data

module private Fakes =
    let private cleanArchitecture = {
        SKU = SKU.CleanArchitecture
        Name = "Clean Architecture: A Craftsman's Guide to Software Structure and Design"
        Description =
            "Building upon the success of best-sellers The Clean Coder and Clean Code, legendary software craftsman Robert C. 'Uncle Bob' Martin "
            + "shows how to bring greater professionalism and discipline to application architecture and design."
        ImageUrl = ImageUrl.Valid "https://m.media-amazon.com/images/I/71stxGw9JgL._SY385_.jpg"
    }

    let private domainDrivenDesign = {
        SKU = SKU.DomainDrivenDesign
        Name = "Domain-Driven Design: Tackling Complexity in the Heart of Software"
        Description =
            "Leading software designers have recognized domain modeling and design as critical topics for at least twenty years, "
            + "yet surprisingly little has been written about what needs to be done or how to do it. Although it has never been "
            + "clearly formulated, a philosophy has developed as an undercurrent in the object community, which I call 'domain-driven design'."
        ImageUrl = ImageUrl.Valid "https://m.media-amazon.com/images/I/81ykBjVaUjL._SY425_.jpg"
    }

    let private domainModelingMadeFunctional = {
        SKU = SKU.DomainModelingMadeFunctional
        Name = "Domain Modeling Made Functional: Tackle Software Complexity with Domain-Driven Design and F#"
        Description =
            "You want increased IPSer satisfaction, faster development cycles, and less wasted work. Domain-driven design (DDD) combined "
            + "with functional programming is the innovative combo that will get you there. In this pragmatic, down-to-earth guide, you'll see "
            + "how applying the core principles of functional programming can result in software designs that model real-world requirements "
            + "both elegantly and concisely - often more so than an object-oriented approach. Practical examples in the open-source F# "
            + "functional language, and examples from familiar business domains, show you how to apply these techniques to build software "
            + "that is business-focused, flexible, and high quality."
        ImageUrl = ImageUrl.Valid "https://m.media-amazon.com/images/I/91THtohxBjL._SY385_.jpg"
    }

    let private javaScriptTheGoodParts = {
        SKU = SKU.JavaScriptTheGoodParts
        Name = "JavaScript: The Good Parts"
        Description =
            "Most programming languages contain good and bad parts, but JavaScript has more than its share of the bad, having been developed "
            + "and released in a hurry before it could be refined. This authoritative book scrapes away these bad features to reveal a subset "
            + "of JavaScript that's more reliable, readable, and maintainable than the language as a whole—a subset you can use to create "
            + "truly extensible and efficient code."
        ImageUrl = ImageUrl.Valid "https://m.media-amazon.com/images/I/91YlBt-bCHL._SY385_.jpg"
    }

    let private thePragmaticProgrammer = {
        SKU = SKU.ThePragmaticProgrammer
        Name = "The Pragmatic Programmer: Your Journey to Mastery"
        Description =
            "The Pragmatic Programmer is one of those rare tech books you'll read, re-read, and read again over the years. "
            + "Whether you're new to the field or an experienced practitioner, you'll come away with fresh insights each and every time."
        ImageUrl = ImageUrl.Valid "https://m.media-amazon.com/images/I/911WvX7M98L._SY385_.jpg"
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

    let getProducts () =
        async {
            do! Async.Sleep(millisecondsDueTime = 500) // Simulate latency
            return repository.Values |> Seq.toList
        }

    let getProduct sku =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency
            let product = repository.Values |> Seq.tryFind (fun x -> x.SKU = sku)
            return product
        }

    let saveProduct (product: Product) =
        async {
            do! Async.Sleep(millisecondsDueTime = 400) // Simulate latency
            return repository |> Dictionary.tryUpdateBy _.SKU product |> liftDataRelatedError
        }