module Shopfoo.Shared.Translations

open System
open Shopfoo.Domain.Types.Products
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

    type About internal (?translations) =
        inherit Base(PageCode.About, ?translations = translations)
        member this.Disclaimer = this.Get "Disclaimer"

    type Home internal (?translations) =
        inherit Base(PageCode.Home, ?translations = translations)

        member this.About = this.Get "About"
        member this.Login = this.Get "Login"
        member this.Logout = this.Get "Logout"
        member this.Page = this.Get "Page"
        member this.Product = this.Get "Product"
        member this.Products = this.Get "Products"

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
            |}

        member this.ThemeGroup =
            this.WithPrefix "ThemeGroup."
            <| fun this -> {|
                Dark = this.Get "Dark" // ↩
                Light = this.Get "Light"
            |}

    type Login internal (?translations) =
        inherit Base(PageCode.Login, ?translations = translations)
        member this.SelectDemoUser = this.Get "SelectDemoUser"

    type Product internal (?translations) =
        inherit Base(PageCode.Product, ?translations = translations)
        member this.CatalogInfo = this.Get "CatalogInfo"
        member this.Description = this.Get "Description"
        member this.ImageUrl = this.Get "ImageUrl"
        member this.Name = this.Get "Name"
        member this.Save = this.Get "Save"
        member this.SaveOk(SKU sku) = this.Format("SaveOk", sku)
        member this.SaveError(SKU sku, error: string) = this.Format("SaveError", sku, error)

open TranslationPages

type private Section =
    | Section of string

    static member About = Section(nameof About)
    static member Home = Section(nameof Home)
    static member Login = Section(nameof Login)
    static member Product = Section(nameof Product)

type AppTranslations
    private
    (
        about: About, // ↩
        home: Home,
        login: Login,
        product: Product,
        ?translations
    ) =
    let sections = [
        Section.About, about :> Base
        Section.Home, home
        Section.Login, login
        Section.Product, product
    ]

    let pageCodes predicate =
        sections
        |> Seq.map snd
        |> Seq.distinct
        |> Seq.filter predicate
        |> Seq.map _.PageCode
        |> Set

    new() = AppTranslations(About(), Home(), Login(), Product())

    member val About = about
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
            recreatePageIfNeeded about (fun () -> About translations),
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