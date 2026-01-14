[<RequireQualifiedAccess>]
module internal Shopfoo.Product.Data.Books

open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Errors
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Product.Data

[<AutoOpen>]
module internal Dto =
    type AuthorDto = { Id: OLID; Name: string }

    type BookDto = {
        ISBN: ISBN
        Title: string
        Subtitle: string
        Description: string
        Authors: AuthorDto list
        Image: string
        Tags: string list
    }

module private Db =
    let andyHunt = { Id = OLID "OL1391034A"; Name = "Andy Hunt" }
    let daveThomas = { Id = OLID "OL1439324A"; Name = "Dave Thomas" }
    let douglasCrockford = { Id = OLID "OL14426346A"; Name = "Douglas Crockford" }
    let ericEvans = { Id = OLID "OL5205508A"; Name = "Eric Evans" }
    let isaacAbraham = { Id = OLID "OL7495415A"; Name = "Isaac Abraham" }
    let markSeemann = { Id = OLID "OL7079278A"; Name = "Mark Seemann" }
    let martinFowler = { Id = OLID "OL27090A"; Name = "Martin Fowler" }
    let scottWlaschin = { Id = OLID "OL7495494A"; Name = "Scott Wlaschin" }
    let uncleBobMartin = { Id = OLID "OL216228A"; Name = "Robert C. Martin" }
    let vladimirKhorikov = { Id = OLID "OL8466340A"; Name = "Vladimir Khorikov" }

    let private cleanArchitecture = {
        ISBN = ISBN.CleanArchitecture
        Title = "Clean Architecture"
        Subtitle = "A Craftsman's Guide to Software Structure and Design"
        Description = ""
        Authors = [ uncleBobMartin ]
        Image = "https://covers.openlibrary.org/b/id/15093860-L.jpg"
        Tags = [ "Architecture"; "Software Craftsmanship" ]
    }

    let private cleanCode = {
        ISBN = ISBN.CleanCode
        Title = "Clean Code"
        Subtitle = "A Handbook of Agile Software Craftsmanship"
        Description = "Even bad code can function. But if code isn't clean, it can bring a development organization to its knees."
        Authors = [ uncleBobMartin ]
        Image = "https://covers.openlibrary.org/b/id/15126503-L.jpg"
        Tags = [ "OOP"; "Software Craftsmanship" ]
    }

    let private codeThatFitsInYourHead = {
        ISBN = ISBN.CodeThatFitsInYourHead
        Title = "Code That Fits in Your Head"
        Subtitle = "Heuristics for Software Engineering"
        Description = ""
        Authors = [ markSeemann ]
        Image = "https://covers.openlibrary.org/b/id/12848531-L.jpg"
        Tags = [ "Software Craftsmanship" ]
    }

    let private dependencyInjection = {
        ISBN = ISBN.DependencyInjection
        Title = "Dependency Injection"
        Subtitle = "Principles, Practices, and Patterns"
        Description = ""
        Authors = [ markSeemann ]
        Image = "https://covers.openlibrary.org/b/id/8507526-L.jpg"
        Tags = [ "OOP"; "Software Craftsmanship" ]
    }

    let private domainDrivenDesign = {
        ISBN = ISBN.DomainDrivenDesign
        Title = "Domain-Driven Design"
        Subtitle = "Tackling Complexity in the Heart of Software"
        Description =
            "Leading software designers have recognized domain modeling and design as critical topics for at least twenty years, "
            + "yet surprisingly little has been written about what needs to be done or how to do it. Although it has never been "
            + "clearly formulated, a philosophy has developed as an undercurrent in the object community, which I call 'domain-driven design'."
        Authors = [ ericEvans ]
        Image = "https://m.media-amazon.com/images/I/81ykBjVaUjL._SY425_.jpg"
        Tags = [ "Domain-Driven Design" ]
    }

    let private domainDrivenDesignReference = {
        ISBN = ISBN.DomainDrivenDesignReference
        Title = "Domain-Driven Design Reference"
        Subtitle = "Definitions and Pattern Summaries"
        Description = ""
        Authors = [ ericEvans ]
        Image = "https://covers.openlibrary.org/b/id/10482709-L.jpg"
        Tags = [ "Domain-Driven Design" ]
    }

    let private domainModelingMadeFunctional = {
        ISBN = ISBN.DomainModelingMadeFunctional
        Title = "Domain Modeling Made Functional"
        Subtitle = "Tackle Software Complexity with Domain-Driven Design and F#"
        Description =
            "You want increased IPSer satisfaction, faster development cycles, and less wasted work. Domain-driven design (DDD) combined "
            + "with functional programming is the innovative combo that will get you there. In this pragmatic, down-to-earth guide, you'll see "
            + "how applying the core principles of functional programming can result in software designs that model real-world requirements "
            + "both elegantly and concisely - often more so than an object-oriented approach. Practical examples in the open-source F# "
            + "functional language, and examples from familiar business domains, show you how to apply these techniques to build software "
            + "that is business-focused, flexible, and high quality."
        Authors = [ scottWlaschin ]
        Image = "https://m.media-amazon.com/images/I/91THtohxBjL._SY385_.jpg"
        Tags = [ "Domain-Driven Design"; "F#" ]
    }

    let private fsharpInActions = {
        ISBN = ISBN.FsharpInActions
        Title = "F# in Action"
        Subtitle = ""
        Description =
            "F# is engineered to make functional programming practical and accessible. This book will get you started writing your first simple, robust, and high performing functional code."
        Image = "https://covers.openlibrary.org/b/id/14796680-L.jpg"
        Authors = [ isaacAbraham ]
        Tags = [ "F#" ]
    }

    let private howJavaScriptWorks = {
        ISBN = ISBN.HowJavaScriptWorks
        Title = "How JavaScript Works"
        Subtitle = ""
        Description = "Douglas Crockford starts by looking at the fundamentals: names, numbers, booleans, characters, and bottom values."
        Authors = [ douglasCrockford ]
        Image = "https://covers.openlibrary.org/b/id/10189246-L.jpg"
        Tags = [ "JavaScript" ]
    }

    let private javaScriptTheGoodParts = {
        ISBN = ISBN.JavaScriptTheGoodParts
        Title = "JavaScript"
        Subtitle = "The Good Parts"
        Description = "Covers the bits of Javascript that are worth using, and how to use them, as well as the bits that should be avoided. "
        Image = "https://m.media-amazon.com/images/I/91YlBt-bCHL._SY385_.jpg"
        Authors = [ douglasCrockford ]
        Tags = [ "JavaScript" ]
    }

    let private refactoring = {
        ISBN = ISBN.Refactoring
        Title = "Refactoring"
        Subtitle = "Improving the Design of Existing Code"
        Description = "Any fool can write code that a computer can understand. Good programmers write code that humans can understand."
        Image = "https://m.media-amazon.com/images/I/71vEr0oyt-L._SY385_.jpg"
        Authors = [ martinFowler ]
        Tags = [ "Software Craftsmanship" ]
    }

    let private thePragmaticProgrammer = {
        ISBN = ISBN.ThePragmaticProgrammer
        Title = "The Pragmatic Programmer"
        Subtitle = "Your Journey to Mastery"
        Description =
            "The Pragmatic Programmer is one of those rare tech books you'll read, re-read, and read again over the years. "
            + "Whether you're new to the field or an experienced practitioner, you'll come away with fresh insights each and every time."
        Authors = [ andyHunt; daveThomas ]
        Image = "https://m.media-amazon.com/images/I/911WvX7M98L._SY385_.jpg"
        Tags = [ "Software Craftsmanship" ]
    }

    let private unitTesting = {
        ISBN = ISBN.UnitTesting
        Title = "Unit Testing"
        Subtitle = "Principles, Practices, and Patterns"
        Description = ""
        Authors = [ vladimirKhorikov ]
        Image = "https://covers.openlibrary.org/b/id/14572001-L.jpg"
        Tags = [ "Software Craftsmanship"; "Testing" ]
    }

    let all = [
        cleanArchitecture
        cleanCode
        codeThatFitsInYourHead
        dependencyInjection
        domainDrivenDesign
        domainDrivenDesignReference
        domainModelingMadeFunctional
        fsharpInActions
        howJavaScriptWorks
        javaScriptTheGoodParts
        refactoring
        thePragmaticProgrammer
        unitTesting
    ]

module private Mappers =
    module DtoToModel =
        let mapBookDetails (dto: BookDto) : Book = {
            ISBN = dto.ISBN
            Authors = dto.Authors |> List.map (fun authorDto -> { OLID = authorDto.Id; Name = authorDto.Name })
            Subtitle = dto.Subtitle
            Tags = dto.Tags
        }

        let mapBook (dto: BookDto) : Product = {
            SKU = dto.ISBN.AsSKU
            Title = dto.Title
            Description = dto.Description
            Category = Category.Books(mapBookDetails dto)
            ImageUrl = ImageUrl.Valid dto.Image
        }

    module ModelToDto =
        let mapAuthors (book: Book) : AuthorDto list = // ↩
            book.Authors |> List.map (fun author -> { Id = author.OLID; Name = author.Name })

        let mapBook (product: Product) : BookDto =
            match product.Category with
            | Category.Books details -> {
                ISBN = details.ISBN
                Title = product.Title
                Description = product.Description
                Authors = mapAuthors details
                Subtitle = details.Subtitle
                Image = product.ImageUrl.Url
                Tags = details.Tags
              }
            | _ -> failwith $"Invalid category %A{product.Category} for book product."

module internal Pipeline =
    let repository = Db.all |> Dictionary.ofListBy _.ISBN

    let getProducts () =
        async {
            do! Async.Sleep(millisecondsDueTime = 500) // Simulate latency
            return [ for dto in repository.Values -> Mappers.DtoToModel.mapBook dto ]
        }

    let getProduct (isbn: ISBN) =
        async {
            do! Async.Sleep(millisecondsDueTime = 250) // Simulate latency
            let product = repository.Values |> Seq.tryFind (fun x -> x.ISBN = isbn)
            return product |> Option.map Mappers.DtoToModel.mapBook
        }

    let saveProduct product =
        async {
            do! Async.Sleep(millisecondsDueTime = 400) // Simulate latency
            let dto = Mappers.ModelToDto.mapBook product
            return repository |> Dictionary.tryUpdateBy _.ISBN dto |> liftDataRelatedError
        }