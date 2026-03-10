module Shopfoo.Client.Components.Highlight

open System
open Feliz
open Shopfoo.Client
open Shopfoo.Client.Search

[<Flags>]
type HighlightProp =
    | BorderColor = 1
    | BorderSize = 2
    | TextColors = 4

[<RequireQualifiedAccess>]
module HighlightProp =
    [<Literal>]
    let Border = HighlightProp.BorderColor ||| HighlightProp.BorderSize

    [<Literal>]
    let All = Border ||| HighlightProp.TextColors

type Highlighting with
    member this.CssClasses(props: HighlightProp) = [
        match this with
        | Highlighting.None -> ()
        | Highlighting.Active ->
            if props.HasFlag HighlightProp.BorderColor then
                "border-yellow-400"

            if props.HasFlag HighlightProp.BorderSize then
                "rounded-sm border-1"

            if props.HasFlag HighlightProp.TextColors then
                "bg-yellow-200 text-black"
    ]

[<RequireQualifiedAccess>]
module Highlight =
    /// <summary>
    /// Highlights matches of the given <c>SearchTargetResult</c> in the text content of the given React element.
    /// </summary>
    /// <remarks>
    /// The syntax is closed to the Feliz one - see example below.
    /// </remarks>
    /// <example>
    /// Given the `result` of searching "code" in a book title, the following code:
    /// <code lang="fsharp">
    /// result |> Highlight.matches Html.span [
    ///     ...props
    ///     prop.text "Clean Code by Robert C. Martin"
    /// ]
    /// </code>
    ///
    /// ...is equivalent to:
    /// <code lang="fsharp">
    /// Html.span [
    ///     ...props
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
    let matches (element: IReactProperty list -> ReactElement) (props: IReactProperty list) (result: SearchTargetResult) : ReactElement =
        let highlightingClasses = result.Highlighting.CssClasses HighlightProp.All

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
            match result with
            | _ when String.IsNullOrWhiteSpace(result.Text) -> props
            | { Matches = [] } -> [ yield! props; prop.text result.Text ]

            | _ -> [
                prop.key reactKey
                yield! otherProps

                prop.children [
                    for i, m in result.Matches |> List.indexed do
                        match highlightingClasses, m.MatchType with
                        | [], _
                        | _, NoMatch -> // ↩
                            Html.text m.Text

                        | classes, TextMatch ->
                            Html.mark [
                                prop.key $"%s{reactKey}-match-%i{i}"
                                prop.classes classes
                                prop.text m.Text
                            ]
                ]
              ]

        element props