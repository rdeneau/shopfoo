# Shopfoo

Shopfoo is a full-stack web app. It is a demo project showcasing the [Safe Clean Architecture](https://rdeneau.gitbook.io/safe-clean-architecture)—a term I coined—with domain workflows based on pseudo algebraic effects.

> 😉 The name **Shopfoo** is a reference to the [chop suey](https://en.wikipedia.org/wiki/Chop_suey) dish and the [song](https://en.wikipedia.org/wiki/Chop_Suey!) by System of a Down.

## Technical stack

The solution is based on the [SAFEr.Template](https://github.com/Dzoukr/SAFEr.Template). It's written in F# on both Client and Server sides:

- Client:
  - [Fable 4](https://fable.io) F#-to-JavaScript transpiler
  - SPA: [React 19](https://react.dev) under the hood
  - HTML DSL: [Feliz 2.9](https://fable-hub.github.io/Feliz/2.9.0)
  - ELM architecture: [Elmish](https://elmish.github.io/elmish/)
    - MVU loop per page using `React.useElmish` from [Feliz.UseElmish](https://fable-hub.github.io/Feliz/ecosystem/Hooks/Feliz.UseElmish)
    - `FullContext` object stored in the root view and shared to page views
  - Design system: [Feliz.DaisyUI](https://dzoukr.github.io/Feliz.DaisyUI/#/) built on [🌼DaisyUI](https://daisyui.com) and [tailwindcss](https://tailwindcss.com)
  - Build: [⚡Vite.js](https://vite.dev) (instead of webpack)
  - Routing: navigation between pages using [Feliz.Router](https://fable-hub.github.io/Feliz/ecosystem/Components/Feliz.Router)
- Server:
  - [ASP.NET Core](https://www.asp.net/core/overview/aspnet-vnext)
  - [🦒Giraffe](https://giraffe.wiki) as a functional overlay
- Client-Server:
  - [Fable.Remoting](https://zaid-ajaj.github.io/Fable.Remoting/#/) supporting the "Remoting API", with endpoints grouped between `Home` and `Product`
  - Shared `ApiError` type, hiding the `Error` domain type
  - Custom helpers for the calls to the Remoting API:
    - Types: `ApiResult<'a> = Result<'a, ApiError>`, `ApiCall<'a> = Start | Done of ApiResult<'a>`
    - Objects: `fullContext.PrepareRequest(...) : Cmder` → `cmder.ofApiRequest(ApiRequestArgs) : Cmd<Msg>`, abstracting the Elmish `Cmd.OfAsync.either`
  - Translations:
    - Grouped by pages, loaded on demand and cached on the Client side
    - Friendly and strongly-typed syntax for the views: e.g. `translations.Home.Theme.Garden`, `translations.Product.Discount discount.Value`

## Architecture glimpses

> ☝️ **Disclaimer:** The architecture is strongly opinionated and is not a silver-bullet! Of course, you can apply it completely on your projects—as it is fairly comprehensive for code organization, although it is not production ready. You can also just pick some ideas and techniques.

The solution is fully written in F#, a **multi-paradigm** language. The architecture embraces both the functional programming (FP)—the core design of F#—and the object-oriented programming, in a balanced manner.

The architecture combines—or rather is inspired by—many documented architectures:

- *Clean architecture:* domain-centric, multi-layer: Presentation|Infrastructure → Application → Domain
- *Hexagonal architecture:* domain-centric—the hexagon contains the Application and Domain layers—the Presentation layer is at its left—it's the driving part—the Infrastructure is at its right—the driven part.
- *Modular monolith:* multi-domain solution
- *Screaming architecture:* the domain is expressed directly through the file/folder organization
- *Vertical slice architecture:* gathering by features rather than by technical aspects
- *SAFE architecture*: Client-Shared-Server 3-project solution, F# full-stack

Hence its name, ❝ *Safe Clean Architecture*. ❞

## Run the application

Run the relevant command(s) in a console at the root of the solution:

```powershell
# Both Client and Server
dotnet run

# Server only (fast: no dotnet restore)
dotnet run Server

# Client only (fast: no dotnet restore)
yarn start:fast

# Client only, with dotnet restore
yarn start
```

Then, you can browse the application at the URL: http://localhost:8080.

## Features

Shopfoo is the back-office tool to manage a shop selling products, limited to 5 well-known technical books for simplicity's sake.

### Multi-domain features

> 💡 Inspired by [Task based UI](https://youtu.be/DjZepWrAKzM), a YouTube video by Derek Comartin.

Several domains are involved: Catalog, Sales, Purchases, Warehouse.

- Catalog Info
  - [x] Index: table of products
  - [x] Details:
    - [x] Edit the cover image, with preview.
    - [x] Edit the book name: required, max 128 chars.
    - [x] Edit the description: max 512 chars.
- Sales
  - List price (a.k.a. recommended price)
    - [x] Display the price fetched from the server
    - [ ] Action: Define if not defined
    - [ ] Action: Modify if defined (intent: Increase or Decrease)
    - [ ] Action: Remove if defined
  - Retail price
    - [x] Display the price fetched from the server
    - [x] Display discount / list price if any
    - [ ] Display margin / Purchased price (last or average over 1Y)
    - [ ] Action: Modify (intent: Increase or Decrease)
    - [ ] Action: Mark as sold out
  - Sales
    - [ ] Display last sale
    - [ ] Display 1Y sale: quantity, total
    - [ ] Action: Input sales
- Purchasing
  - Purchased price
    - [ ] Display last price and average over 1Y based on the purchases and stock adjustment, with an arrow indicating if it has increased ↗️ or decreased ↘️
    - [ ] Action: Receive purchased products
- Warehouse
  - Stock
    - [ ] Display the stock based on the stock events
    - [ ] Action: Inventory adjustment
      - [ ] Display difference / determined stock: losses or extras

### UI features

- Navigation bar with a breadcrumb composed of clickable segments to go up any level
- User rights:
  - Log in using a persona to demonstrate the access rights
  - Client side: on access denied → hide button or section, use readonly inputs, redirect to an authorized page: login or page not found
  - Server side: exception → not user-friendly but just to prevent hacking
- Localization: instant switch from English to French
- Theme: 4 light themes and 4 dark themes, with colors preview
- `Money` type to handle prices with different currency (`Dollars` and `Euros`)
  - Currency symbol position in the input box: `$ 22.99` vs `22.99 €`
- Form validation

## TODO

- [ ] Demo video
- [ ] Architecture tests
- [ ] Workflow tests
- [ ] UI tests
- [ ] Deploy to Azure or AWS? with Aspire?
- [ ] Full documentation (gitbook), to complete the [Elmish book](https://zaid-ajaj.github.io/the-elmish-book/#/).
