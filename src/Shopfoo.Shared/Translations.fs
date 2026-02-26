module Shopfoo.Shared.Translations

open System
open System.Text.RegularExpressions
open Shopfoo.Common
open Shopfoo.Domain.Types.Catalog
open Shopfoo.Domain.Types.Translations

type DatePartFormat =
    | DayInMonth
    | MonthShortName
    | Year
    | Separator of string

module TranslationPages =
    type internal TranslationKeyBuilder(pageCode: PageCode) =
        member _.Create tag : TranslationKey = // ↩
            { Page = pageCode; Tag = TagCode tag }

        member this.Error name = this.Create $"%s{name}Error"
        member this.ErrorOn action = this.Create $"Error%s{action}"

    type Base internal (pageCode: PageCode, ?translations: Translations, ?buildTagCode: string -> TagCode) =
        let buildTagCode = defaultArg buildTagCode TagCode
        let translations = defaultArg translations Translations.Empty
        let tagMap = translations.Pages |> Map.tryFind pageCode |> Option.defaultValue Map.empty

        member val internal PageCode = pageCode

        // `internal` marks here members shared with child classes, in the absence of `protected` keyword in F#
        member val internal Count = tagMap.Count
        member val internal IsEmpty = tagMap.IsEmpty

        member internal _.TranslationPath(TagCode tag) = $"%A{pageCode}.%s{tag}"

        member internal this.FallbackValue tag = $"[*** %s{this.TranslationPath tag} ***]"

        member internal this.Format(tag: string, [<ParamArray>] args: obj[]) = String.Format(this.Get(tag), args)

        member internal this.Get(tag: string, ?defaultValue) = this.Get(buildTagCode tag, ?defaultValue = defaultValue)

        member internal this.Get(tagCode, ?defaultValue) =
            this.GetOrNone tagCode
            |> Option.orElse defaultValue
            |> Option.defaultWith (fun () -> this.FallbackValue tagCode)

        member internal this.GetOrNone(tag: string) = this.GetOrNone(buildTagCode tag)
        member internal this.GetOrNone(tagCode) = tagMap |> Map.tryFind tagCode

        member internal this.WithPrefix (tagPrefix: string) (useNesting: Base -> _) =
            let newBase = Base(pageCode, translations, buildTagCode = (fun tag -> buildTagCode $"{tagPrefix}{tag}"))
            useNesting newBase

    type Home internal (?translations) =
        inherit Base(PageCode.Home, ?translations = translations)

        member this.About = this.Get "About"
        member this.AboutDisclaimer = this.Get "AboutDisclaimer"
        member this.Admin = this.Get "Admin"
        member this.AdminDisclaimer = this.Get "AdminDisclaimer"
        member this.Bazaar = this.Get "Bazaar"
        member this.Books = this.Get "Books"
        member this.Login = this.Get "Login"
        member this.Logout = this.Get "Logout"
        member this.Page = this.Get "Page"
        member this.Product = this.Get "Product"
        member this.Products = this.Get "Products"
        member this.SelectedPlural = this.Get "SelectedPlural"

        member this.Add = this.Get "Add"
        member this.Cancel = this.Get "Cancel"
        member this.Clear = this.Get "Clear"
        member this.Close = this.Get "Close"
        member this.Save = this.Get "Save"
        member this.Search = this.Get "Search"
        member this.SearchOrAdd = this.Get "SearchOrAdd"

        member this.Confirmation = this.Get "Confirmation"
        member this.Completed = this.Get "Completed"
        member this.Error(error: string) = this.Format("Error", error)
        member this.SavedOk(name: string) = this.Format("SavedOk", name)
        member this.SavedError(name: string, error: string) = this.Format("SavedError", name, error)
        member this.Warning = this.Get "Warning"

        member this.Colon = this.Get "Colon"
        member this.Date = this.Get "Date"
        member this.Required = this.Get "Required"

        member this.ChangeLangError(lang: string, error: string) = this.Format("ChangeLangError", lang, error)
        member this.ChangeLangOk = this.Get "ChangeLangOk"
        member this.ErrorNotFound(what: string) = this.Format("ErrorNotFound", what)

        member private this.DayInMonth =
            function
            | 1 -> this.Get "DayInMonth.1st"
            | 2 -> this.Get "DayInMonth.2nd"
            | 3 -> this.Get "DayInMonth.3rd"
            | 4 -> this.Get "DayInMonth.4th"
            | 5 -> this.Get "DayInMonth.5th"
            | 6 -> this.Get "DayInMonth.6th"
            | 7 -> this.Get "DayInMonth.7th"
            | 8 -> this.Get "DayInMonth.8th"
            | 9 -> this.Get "DayInMonth.9th"
            | 10 -> this.Get "DayInMonth.10th"
            | 11 -> this.Get "DayInMonth.11th"
            | 12 -> this.Get "DayInMonth.12th"
            | 13 -> this.Get "DayInMonth.13th"
            | 14 -> this.Get "DayInMonth.14th"
            | 15 -> this.Get "DayInMonth.15th"
            | 16 -> this.Get "DayInMonth.16th"
            | 17 -> this.Get "DayInMonth.17th"
            | 18 -> this.Get "DayInMonth.18th"
            | 19 -> this.Get "DayInMonth.19th"
            | 20 -> this.Get "DayInMonth.20th"
            | 21 -> this.Get "DayInMonth.21st"
            | 22 -> this.Get "DayInMonth.22nd"
            | 23 -> this.Get "DayInMonth.23rd"
            | 24 -> this.Get "DayInMonth.24th"
            | 25 -> this.Get "DayInMonth.25th"
            | 26 -> this.Get "DayInMonth.26th"
            | 27 -> this.Get "DayInMonth.27th"
            | 28 -> this.Get "DayInMonth.28th"
            | 29 -> this.Get "DayInMonth.29th"
            | 30 -> this.Get "DayInMonth.30th"
            | 31 -> this.Get "DayInMonth.31st"
            | _ -> String.Empty

        member private this.ShortMonth =
            function
            | 1 -> this.Get "ShortMonth.Jan"
            | 2 -> this.Get "ShortMonth.Feb"
            | 3 -> this.Get "ShortMonth.Mar"
            | 4 -> this.Get "ShortMonth.Apr"
            | 5 -> this.Get "ShortMonth.May"
            | 6 -> this.Get "ShortMonth.Jun"
            | 7 -> this.Get "ShortMonth.Jul"
            | 8 -> this.Get "ShortMonth.Aug"
            | 9 -> this.Get "ShortMonth.Sep"
            | 10 -> this.Get "ShortMonth.Oct"
            | 11 -> this.Get "ShortMonth.Nov"
            | 12 -> this.Get "ShortMonth.Dec"
            | _ -> String.Empty

        /// Parse the format parts from the translation, e.g. "DayInMonth ShortMonth, Year" -> [DayInMonth; Separator " "; ShortMonth; Separator ", "; Year]
        member this.StandardDateFormat: DatePartFormat list = [
            for part in Regex.Split(input = this.Get("StandardDateFormat"), pattern = @"(DayInMonth|DDD|ShortMonth|MMM|Year|YYYY)") do
                match part with
                | null
                | "" -> ()
                // ReSharper disable FSharpRedundantParens
                | ("DDD" | "DayInMonth") -> DayInMonth
                | ("MMM" | "ShortMonth") -> MonthShortName
                | ("YYYY" | "Year") -> Year
                // ReSharper restore FSharpRedundantParens
                | separator -> Separator separator
        ]

        member this.FormatDate(date: DateOnly, format: DatePartFormat list) =
            String.concat "" [
                for part in format do
                    match part with
                    | DayInMonth -> this.DayInMonth date.Day
                    | MonthShortName -> this.ShortMonth date.Month
                    | Year -> string date.Year
                    | Separator sep -> sep
            ]

        member this.Theme =
            this.WithPrefix "Theme."
            <| fun this -> {|
                Dark = this.Get "Dark"
                Light = this.Get "Light"
                Business = this.Get "Business"
                Corporate = this.Get "Corporate"
                Garden = this.Get "Garden"
                Night = this.Get "Night"
                Nord = this.Get "Nord"
                Dim = this.Get "Dim"
            |}

        member this.ThemeGroup =
            this.WithPrefix "ThemeGroup."
            <| fun this -> {| // ↩
                Dark = this.Get "Dark"
                Light = this.Get "Light"
            |}

    type Login internal (?translations) =
        inherit Base(PageCode.Login, ?translations = translations)

        member this.Access =
            this.WithPrefix "Access."
            <| fun this -> {| // ↩
                Edit = this.Get "Edit"
                View = this.Get "View"
            |}

        member this.Feat =
            this.WithPrefix "Feat."
            <| fun this -> {|
                About = this.Get "About"
                Admin = this.Get "Admin"
                Catalog = this.Get "Catalog"
                Sales = this.Get "Sales"
                Warehouse = this.Get "Warehouse"
            |}

        member this.SelectPersona = this.Get "SelectPersona"
        member this.Persona = this.Get "Persona"

    type Product internal (?translations) =
        inherit Base(PageCode.Product, ?translations = translations)

        member this.Actions = this.Get "Actions"
        member this.Authors = this.Get "Authors"
        member this.CatalogInfo = this.Get "CatalogInfo"
        member this.Category = this.Get "Category"
        member this.Description = this.Get "Description"
        member this.HighlightMatches = this.Get "HighlightMatches"
        member this.MatchCase = this.Get "MatchCase"
        member this.ImageUrl = this.Get "ImageUrl"
        member this.Name = this.Get "Name"
        member this.NewProductTitle = this.Get "NewProductTitle"
        member this.NewProductDisclaimer = this.Get "NewProductDisclaimer"
        member this.SoldOut = this.Get "SoldOut"
        member this.Subtitle = this.Get "Subtitle"
        member this.Tags = this.Get "Tags"
        member this.AddTag = this.Get "AddTag"
        member this.SearchAuthors = this.Get "SearchAuthors"
        member this.NoAuthorsFound = this.Get "NoAuthorsFound"
        member this.AuthorSearchLimit(limit: int, totalFound: int) = this.Format("AuthorSearchLimit", limit, totalFound)
        member this.SearchBooks = this.Get "SearchBooks"
        member this.NoBooksFound = this.Get "NoBooksFound"
        member this.BookSearchLimit(limit: int, totalFound: int) = this.Format("BookSearchLimit", limit, totalFound)
        member this.OpenLibraryAuthorPage(authorName: string) = this.Format("OpenLibraryAuthorPage", authorName)

        member this.Discount = this.Get "Discount"
        member this.Margin = this.Get "Margin"
        member this.Quantity = this.Get "Quantity"

        member this.Increase = this.Get "Increase"
        member this.Decrease = this.Get "Decrease"
        member this.Define = this.Get "Define"

        member this.CurrentPrice = this.Get "CurrentPrice"
        member this.Price = this.Get "Price"
        member this.NewPrice = this.Get "NewPrice"
        member this.ListPrice = this.Get "ListPrice"
        member this.RetailPrice = this.Get "RetailPrice"
        member this.SalePrice = this.Get "SalePrice"

        member this.AverageOver1Y = this.Get "AverageOver1Y"
        member this.LastPurchase = this.Get "LastPurchase"
        member this.LastSale = this.Get "LastSale"
        member this.TotalSalesOver1Y = this.Get "TotalSalesOver1Y"

        member this.PriceAction =
            this.WithPrefix "PriceAction."
            <| fun this -> {|
                Define = this.Get "Define"
                Remove = this.Get "Remove"
                Increase = this.Get "Increase"
                Decrease = this.Get "Decrease"
                MarkAsSoldOut = this.Get "MarkAsSoldOut"
                WarnMarkAsSoldOutForbidden = this.Get "WarnMarkAsSoldOutForbidden"
                MarkAsSoldOutDialog =
                    this.WithPrefix "MarkAsSoldOutDialog."
                    <| fun this -> {| Confirm = this.Get "Confirm"; Question = this.Get "Question" |}
                RemoveListPriceDialog =
                    this.WithPrefix "RemoveListPriceDialog."
                    <| fun this -> {| Confirm = this.Get "Confirm"; Question = this.Get "Question" |}
            |}

        member this.StoreCategory =
            this.WithPrefix "StoreCategory."
            <| fun this -> {|
                Clothing = this.Get "Clothing"
                Electronics = this.Get "Electronics"
                Jewelry = this.Get "Jewelry"
            |}

        member this.StoreCategoryOf storeCategory =
            match storeCategory with
            | BazaarCategory.Clothing -> this.StoreCategory.Clothing
            | BazaarCategory.Electronics -> this.StoreCategory.Electronics
            | BazaarCategory.Jewelry -> this.StoreCategory.Jewelry

        member this.Stock = this.Get "Stock"
        member this.StockAfterInventory = this.Get "StockAfterInventory"
        member this.StockBeforeInventory = this.Get "StockBeforeInventory"
        member this.SupplyDate = this.Get "SupplyDate"
        member this.SupplyQuantity = this.Get "SupplyQuantity"
        member this.SupplyPurchasePrice = this.Get "SupplyPurchasePrice"

        member this.StockAction =
            this.WithPrefix "StockAction."
            <| fun this -> {|
                AdjustStockAfterInventory = this.Get "AdjustStockAfterInventory"
                ReceivePurchasedProducts = this.Get "ReceivePurchasedProducts"
            |}

        member this.SaleAction =
            this.WithPrefix "SaleAction." // ↩
            <| fun this -> {| InputSales = this.Get "InputSales" |}

open TranslationPages

type private Section =
    | Section of string

    static member Home = Section(nameof Home)
    static member Login = Section(nameof Login)
    static member Product = Section(nameof Product)

type AppTranslations
    private
    (
        home: Home, // ↩
        login: Login,
        product: Product,
        ?translations
    ) =
    let sections = [
        Section.Home, home :> Base
        Section.Login, login
        Section.Product, product
    ]

    let pageCodes predicate =
        sections // ↩
        |> Seq.map snd
        |> Seq.distinct
        |> Seq.filter predicate
        |> Seq.map _.PageCode
        |> Set

    new() = AppTranslations(Home(), Login(), Product())

    member val Home = home
    member val Login = login
    member val Product = product

    member val Translations = defaultArg translations Translations.Empty

    member this.Fill(appTranslations: AppTranslations) = // ↩
        this.Fill(appTranslations.Translations)

    member _.Fill(translations: Translations) =
        let recreatePageIfNeeded (page: #Base) createPage =
            if Map.containsKey page.PageCode translations.Pages then
                createPage ()
            else
                page

        AppTranslations(
            recreatePageIfNeeded home (fun () -> Home translations),
            recreatePageIfNeeded login (fun () -> Login translations),
            recreatePageIfNeeded product (fun () -> Product translations),
            translations
        )

    member val EmptyPages = pageCodes _.IsEmpty
    member val PopulatedPages = pageCodes (fun x -> not x.IsEmpty)

    member this.IsEmpty = this.PopulatedPages.IsEmpty

    member val DebugInfo =
        sections
        |> Seq.groupBy (fun (_, info) -> info.PageCode)
        |> Seq.map (fun (pageCode, infos) ->
            let count = infos |> Seq.map (snd >> _.Count) |> Seq.head
            let sections = infos |> Seq.map (fun (Section section, _) -> section) |> String.concat ", "

            let sectionsIfRelevant =
                if sections <> $"%A{pageCode}" then
                    $" [%s{sections}]"
                else
                    ""

            $"%A{pageCode} (%i{count})%s{sectionsIfRelevant}"
        )
        |> Seq.toArray

let (|TranslationsMissing|_|) pageCode (translations: AppTranslations) = // ↩
    translations.PopulatedPages.Contains pageCode |> not |> Option.ofBool