module Shopfoo.Client.Components.Highlight

open System
open System.Text.RegularExpressions
open Feliz
open Shopfoo.Client
open Shopfoo.Common

[<RequireQualifiedAccess>]
module Css =
    [<Literal>]
    let BorderColor = "border-yellow-400"

    [<Literal>]
    let Border = "rounded-sm border-1 " + BorderColor

    [<Literal>]
    let TextColors = "bg-yellow-200 text-black"

/// <summary>
/// Highlights occurrences of <c>searchTerm</c> in the text content of the given React element.
/// </summary>
/// <remarks>
/// The syntax is closed to the Feliz one, just with <c>Highlight.element xxx</c> at the beginning.
/// </remarks>
/// <example>
/// <code lang="fsharp">
/// Highlight.element "code" Html.span [
///     prop.key "title"
///     prop.text "Clean Code by Robert C. Martin"
/// ]
/// </code>
/// Renders as:
/// <code lang="fsharp">
/// Html.span [
///     prop.key "title"
///     prop.children [
///         Html.text "Clean "
///         Html.mark [
///             prop.key "title-match-0"
///             prop.className "bg-yellow-200 text-black rounded-sm border-2 border-yellow-400"
///             prop.text "Code"
///         ]
///         Html.text " by Robert C. Martin"
///     ]
/// ]
/// </code>
/// </example>
let element (searchTerm: string option) (element: IReactProperty list -> ReactElement) (props: IReactProperty list) : ReactElement =
    let fullText =
        props // ↩
        |> List.tryPick (|ReactText|_|)
        |> Option.map string

    let reactKey =
        props
        |> List.tryPick (|ReactKey|_|)
        |> Option.map string
        |> Option.defaultValue (Guid.NewGuid().ToString().[..7])

    let otherProps =
        props
        |> List.filter (
            function
            | ReactKey _
            | ReactText _ -> false
            | _ -> true
        )

    let props =
        match searchTerm, fullText with
        | (None | Some String.NullOrWhiteSpace), _
        | _, (None | Some String.NullOrWhiteSpace) ->
            // No highlighting
            props

        | Some term, Some fullText -> [
            // Highlight occurrences of 'term' in 'fullText' (case-insensitive)

            // If fullText = "Clean Code" and term = "code" then
            // - parts = [| "Clean "; "" |]
            // - matches = [| "Code" |]
            // Result: <span>Clean </span><mark class="...">Code</mark>
            let pattern = Regex.Escape(term)
            let options = RegexOptions.IgnoreCase ||| RegexOptions.Multiline
            let parts = Regex.Split(fullText, pattern, options)
            let matches = Regex.Matches(fullText, pattern, options) |> Seq.toArray

            prop.key reactKey
            yield! otherProps

            prop.children [
                for i in 0 .. parts.Length - 1 do
                    // Regular text
                    Html.text parts[i]

                    // Matched text (highlighted)
                    if i < matches.Length then
                        Html.mark [
                            prop.key $"%s{reactKey}-match-%i{i}"
                            prop.className $"%s{Css.TextColors} %s{Css.Border}"
                            prop.text matches[i].Value
                        ]
            ]
          ]

    element props