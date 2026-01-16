module Shopfoo.Client.Components.Highlight

open System
open Feliz
open Shopfoo.Client
open Shopfoo.Client.Filters

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
/// The syntax is closed to the Feliz one - see example below.
/// </remarks>
/// <example>
/// Given `xxxMatchTexts` the result of search on "code" performed on a book title, the following code:
/// <code lang="fsharp">
/// xxxMatchTexts |> Highlight.matches Html.span [
///     prop.key "title"
///     prop.text "Clean Code by Robert C. Martin"
/// ]
/// </code>
///
/// ...is equivalent to:
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
let matches (element: IReactProperty list -> ReactElement) (props: IReactProperty list) (matchTexts: MatchTexts<_>) : ReactElement =
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
        match matchTexts with
        | _ when String.IsNullOrWhiteSpace(matchTexts.Text) -> props
        | { Matches = [] } -> [ yield! props; prop.text matchTexts.Text ]

        | _ -> [
            prop.key reactKey
            yield! otherProps

            prop.children [
                for i, m in matchTexts.Matches |> List.indexed do
                    match m with
                    | NoMatch, s -> Html.text s
                    | TextMatch, s ->
                        Html.mark [
                            prop.key $"%s{reactKey}-match-%i{i}"
                            prop.className $"%s{Css.TextColors} %s{Css.Border}"
                            prop.text s
                        ]
            ]
          ]

    element props