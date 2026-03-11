namespace Shopfoo.Client.Tests

open Shopfoo.Client.Components.Lang
open Shopfoo.Client.Components.Theme
open Shopfoo.Client.Remoting
open Shopfoo.Client.Routing
open Shopfoo.Client.Tests.Types
open Shopfoo.Client.View
open Shopfoo.Domain.Types
open Shopfoo.Domain.Types.Translations
open Shopfoo.Shared.Errors
open Shopfoo.Shared.Remoting
open Swensen.Unquote
open TUnit.Core

module private AppTestsHelpers =
    let defaultModel: Model = {
        Page = Page.Login
        Theme = Theme.Light
        LangMenus = LangMenu.all
        Toast = None
        FullContext = FullContext.Default.WithUnitTestSession DelayedMessageHandling.Drop
    }

    type LangStatus = Map<Lang, Remote<unit>>

    module LangStatus =
        let allLoaded: LangStatus =
            Map [
                Lang.English, Remote.Loaded()
                Lang.French, Remote.Loaded()
                Lang.Latin, Remote.Loaded()
            ]

        let allOfModel (model: Model) : LangStatus =
            Map [
                for x in model.LangMenus do
                    x.Lang, x.Status
            ]

open AppTestsHelpers

type AppShould() =
    [<Test; MethodDataSource(typeof<LangSet>, nameof LangSet.All)>]
    member _.``indicate that target lang is loading`` targetLang =
        let expectedMenus = LangStatus.allLoaded |> Map.add targetLang Remote.Loading

        let newModel, _ =
            defaultModel // ↩
            |> update (Msg.ChangeLang(targetLang, Start))

        newModel |> LangStatus.allOfModel =! expectedMenus

    [<Test>]
    [<Arguments(Lang.Enum.English, "About")>]
    [<Arguments(Lang.Enum.French, "À propos")>]
    member _.``populate the FullContext after a ChangeLang success message``(Lang.FromEnum lang, about) =
        let { FullContext = actual }, _ =
            defaultModel
            |> update (Msg.ChangeLang(lang, Done(Ok { Lang = lang; Translations = Translations.In lang })))

        (actual.Lang, actual.Translations.Home.About, actual.Translations.PopulatedPages)
        =! (lang, about, Translations.AllPages)

    [<Test>]
    [<Arguments(Lang.Enum.English, "About", Lang.Enum.French)>]
    [<Arguments(Lang.Enum.French, "À propos", Lang.Enum.English)>]
    member _.``preserve the existing FullContext after a ChangeLang failure message``(Lang.FromEnum initialLang, about, Lang.FromEnum targetLang) =
        let apiError = ApiError.Technical "Network error"
        let expectedMenus = LangStatus.allLoaded |> Map.add targetLang (Remote.LoadError apiError)

        let newModel, _ =
            { defaultModel with FullContext = defaultModel.FullContext.WithTranslations(Translations.In initialLang) }
            |> update (Msg.ChangeLang(targetLang, Done(Error apiError)))

        (newModel.FullContext.Lang, newModel.FullContext.Translations.Home.About, newModel |> LangStatus.allOfModel)
        =! (initialLang, about, expectedMenus)

    [<Test; MethodDataSource(typeof<LangSet>, nameof LangSet.All)>]
    member _.``merge new translations with existing ones`` lang =
        let translations = Translations.In lang

        let newModel, _ =
            { defaultModel with FullContext = defaultModel.FullContext.WithTranslations(translations.For(PageCode.Home)) }
            |> update (Msg.FillTranslations(translations.For(PageCode.Product)))

        newModel.FullContext.Translations.PopulatedPages =! Set [ PageCode.Home; PageCode.Product ]

    [<Test; MethodDataSource(typeof<LangSet>, nameof LangSet.All)>]
    member _.``prepare query to fetch translations for the empty pages`` lang =
        let translations = Translations.In(lang).For(PageCode.Home)
        let model = { defaultModel with FullContext = defaultModel.FullContext.WithTranslations translations }

        let _, request = model.FullContext.PrepareQueryWithTranslations()
        request.Body.TranslationPages =! Set [ PageCode.Login; PageCode.Product ]