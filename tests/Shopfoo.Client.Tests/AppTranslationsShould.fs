namespace Shopfoo.Client.Tests

open Shopfoo.Client.Tests.Types
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Translations
open Shopfoo.Tests.Common
open Swensen.Unquote
open TUnit.Core

type AppTranslationsShould() =
    [<Test>]
    member _.``be empty with all page empty given no pages are populated``() =
        let sut = AppTranslations()

        test <@ sut.IsEmpty @>
        test <@ sut.EmptyPages = Translations.AllPages @>
        test <@ sut.PopulatedPages.IsEmpty @>

    [<Test; MethodDataSource(typeof<LangSet>, nameof LangSet.All)>]
    member _.``fill Home translations`` lang =
        let translations = Translations.In(lang).For(PageCode.Home)
        let sut = AppTranslations().Fill translations

        test <@ sut.PopulatedPages.Contains PageCode.Home @>
        test <@ not (sut.PopulatedPages.Contains PageCode.Product) @>
        test <@ not sut.IsEmpty @>
        sut.EmptyPages =! Set [ PageCode.Login; PageCode.Product ]

        let isHomePageMissing =
            match sut with
            | TranslationsMissing PageCode.Home -> true
            | _ -> false

        let isProductPageMissing =
            match sut with
            | TranslationsMissing PageCode.Product -> true
            | _ -> false

        test <@ not isHomePageMissing && isProductPageMissing @>

    [<Test; MethodDataSource(typeof<LangSet>, nameof LangSet.All)>]
    member _.``fill all translations`` lang =
        let translations = Translations.In lang
        let sut = AppTranslations().Fill(translations)

        sut.PopulatedPages =! Translations.AllPages
        sut.EmptyPages =! Set.empty
        test <@ not sut.IsEmpty @>

    [<Test; MethodDataSource(typeof<LangSet>, nameof LangSet.All)>]
    member _.``preserve existing pages when filling another page`` lang =
        let translations = Translations.In lang
        let sut = AppTranslations().Fill(translations.For(PageCode.Home)).Fill(translations.For(PageCode.Login))

        test <@ sut.PopulatedPages.Contains PageCode.Home @>
        test <@ sut.PopulatedPages.Contains PageCode.Login @>
        test <@ not (sut.PopulatedPages.Contains PageCode.Product) @>

    [<Test>]
    member _.``replace translations given another lang``() =
        let enTranslations = Translations.In Lang.English
        let frTranslations = Translations.In Lang.French

        let sut = AppTranslations().Fill(enTranslations)
        assume <@ sut.Home.SelectedPlural = "Selected" @>

        let sut = sut.Fill(frTranslations)
        test <@ sut.Home.SelectedPlural = "Sélectionnés" @>