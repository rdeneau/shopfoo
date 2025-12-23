[<AutoOpen>]
module Shopfoo.Client.UI

open System
open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Feliz
open Feliz.Router
open Shopfoo.Client.Routing
open Shopfoo.Domain.Types.Errors
open Shopfoo.Shared.Translations

type React =
    static member withKey (key: string) (element: ReactElement) =
        ReactBindings.React.createElement ( // ↩
            comp = ReactBindings.React.Fragment,
            props = createObj [ "key" ==> key ],
            children = [ element ]
        )

    static member withKeyAuto(element: ReactElement) =
        let key = Guid.NewGuid().ToString().[..7]
        element |> React.withKey key

type prop with
    static member inline hrefRouted(PageUrl pageUrl) = [
        prop.href (Router.formatPath (pageUrl.Segments, queryString = pageUrl.Query))
        prop.onClick Router.goToUrl
    ]

    /// Adds a single child with an automatically generated key.
    static member inline child(element: ReactElement) =
        prop.children [ element |> React.withKeyAuto ]

type GuardProps(criteria: GuardCriteria, value: string, translations: AppTranslations) =
    let len = String.length value

    member _.textCharCount = [
        match criteria.MaxLength with
        | Some maxLength ->
            prop.text $"%i{len} / %i{maxLength}"

            if len > maxLength then
                prop.className "text-error"
        | _ -> ()
    ]

    member _.textRequired = [
        if criteria.Required then
            prop.text $" (%s{translations.Home.Required})"

            if len = 0 then
                prop.className "text-error"
    ]

    member _.validation = [
        if criteria.Required then
            prop.required true

            if len = 0 then
                prop.className "input-error"

        match criteria.MinLength with
        | Some minLength ->
            prop.minLength minLength

            if len < minLength then
                prop.className "input-error"
        | _ -> ()

        match criteria.MaxLength with
        | Some maxLength ->
            prop.maxLength maxLength

            if len > maxLength then
                prop.className "input-error"
        | _ -> ()
    ]

    member _.value = prop.value value

type GuardCriteria with
    member this.props(value, translations) = GuardProps(this, value, translations)

type Html with
    static member inline a(text: string, p: Page) =
        Html.a [
            yield! prop.hrefRouted p
            prop.key $"{p.Key}"
            prop.text text
        ]

    static member inline classed fn (cn: string) (elm: ReactElement list) =
        fn [ // ↩
            prop.className cn
            prop.children elm
        ]

    static member inline divClassed (cn: string) (elm: ReactElement list) = // ↩
        Html.classed Html.div cn elm

[<RequireQualifiedAccess>]
module JS =
    /// Remove the focus on the active element.
    /// Helpful to properly hide the menu after click and mouse out events.
    let blurActiveElement () =
        match Browser.Dom.document.activeElement with
        | :? Browser.Types.HTMLElement as el -> el.blur ()
        | _ -> ()

    let runAfter (delay: TimeSpan) f =
        let milliseconds = delay.TotalMilliseconds |> int |> max 0
        JS.setTimeout f milliseconds |> ignore

[<RequireQualifiedAccess>]
module Cmd =
    let ofMsgDelayed (msg: 'msg) (delay: TimeSpan) =
        let effect dispatch =
            JS.runAfter delay (fun () -> dispatch msg)

        Cmd.ofEffect effect