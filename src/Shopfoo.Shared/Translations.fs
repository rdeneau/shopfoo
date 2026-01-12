module Shopfoo.Shared.Translations

open System
open Shopfoo.Common
open Shopfoo.Domain.Types.Translations

module TranslationPages =
    type internal TranslationKeyBuilder(pageCode: PageCode) =
        member _.Create tag : TranslationKey = // ↩
            { Page = pageCode; Tag = TagCode tag }

        member this.Error name = this.Create $"%s{name}Error"
        member this.ErrorOn action = this.Create $"Error%s{action}"

    type Base internal (pageCode: PageCode, ?translations: Translations, ?buildTagCode: string -> TagCode) =
        let buildTagCode = defaultArg buildTagCode TagCode

        let translations = defaultArg translations Translations.Empty

        let tagMap =
            translations.Pages |> Map.tryFind pageCode |> Option.defaultValue Map.empty

        member val internal PageCode = pageCode

        // `internal` marks here members shared with child classes, in the absence of `protected` keyword in F#
        member val internal Count = tagMap.Count
        member val internal IsEmpty = tagMap.IsEmpty

        member internal _.TranslationPath(TagCode tag) = $"%A{pageCode}.%s{tag}"

        member internal this.FallbackValue tag =
            $"[*** %s{this.TranslationPath tag} ***]"

        member internal this.Format(tag: string, [<ParamArray>] args: obj[]) = String.Format(this.Get(tag), args)

        member internal this.Get(tag: string, ?defaultValue) =
            this.Get(buildTagCode tag, ?defaultValue = defaultValue)

        member internal this.Get(tagCode, ?defaultValue) =
            this.GetOrNone tagCode
            |> Option.orElse defaultValue
            |> Option.defaultWith (fun () -> this.FallbackValue tagCode)

        member internal this.GetOrNone(tag: string) = this.GetOrNone(buildTagCode tag)
        member internal this.GetOrNone(tagCode) = tagMap |> Map.tryFind tagCode

        member internal this.WithPrefix (tagPrefix: string) (useNesting: Base -> _) =
            let newBase =
                Base(pageCode, translations, buildTagCode = (fun tag -> buildTagCode $"{tagPrefix}{tag}"))

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

        member this.Cancel = this.Get "Cancel"
        member this.Close = this.Get "Close"
        member this.Confirmation = this.Get "Confirmation"
        member this.Completed = this.Get "Completed"
        member this.Error(error: string) = this.Format("Error", error)
        member this.Save = this.Get "Save"
        member this.SaveOk(name: string) = this.Format("SaveOk", name)
        member this.SaveError(name: string, error: string) = this.Format("SaveError", name, error)
        member this.Warning = this.Get "Warning"

        member this.Colon = this.Get "Colon"
        member this.Required = this.Get "Required"

        member this.ChangeLangError(lang: string, error: string) =
            this.Format("ChangeLangError", lang, error)

        member this.ChangeLangOk = this.Get "ChangeLangOk"

        member this.ErrorNotFound(what: string) = this.Format("ErrorNotFound", what)

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
        member this.ImageUrl = this.Get "ImageUrl"
        member this.Name = this.Get "Name"
        member this.SoldOut = this.Get "SoldOut"

        member this.Discount = this.Get "Discount"
        member this.Increase = this.Get "Increase"
        member this.Decrease = this.Get "Decrease"
        member this.Define = this.Get "Define"
        member this.Price = this.Get "Price"
        member this.CurrentPrice = this.Get "CurrentPrice"
        member this.NewPrice = this.Get "NewPrice"
        member this.ListPrice = this.Get "ListPrice"
        member this.RetailPrice = this.Get "RetailPrice"

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

        member this.Stock = this.Get "Stock"
        member this.StockAfterInventory = this.Get "StockAfterInventory"
        member this.StockBeforeInventory = this.Get "StockBeforeInventory"

        member this.StockAction =
            this.WithPrefix "StockAction."
            <| fun this -> {| AdjustStockAfterInventory = this.Get "AdjustStockAfterInventory" |}

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
        |> List.groupBy (fun (_, info) -> info.PageCode)
        |> List.map (fun (pageCode, infos) ->
            let count = infos |> Seq.map (snd >> _.Count) |> Seq.head

            let sections =
                infos |> Seq.map (fun (Section section, _) -> section) |> String.concat ", "

            let sectionsIfRelevant =
                if sections <> $"%A{pageCode}" then
                    $" [%s{sections}]"
                else
                    ""

            $"%A{pageCode} (%i{count})%s{sectionsIfRelevant}"
        )
        |> Seq.toArray

let (|TranslationsMissing|_|) pageCode (translations: AppTranslations) =
    translations.PopulatedPages.Contains pageCode |> not |> Option.ofBool