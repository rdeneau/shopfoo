# AGENTS.md

## Agent Implementation Guardrails

- **Plan-First Protocol:** Whenever you are in Plan Mode, in a `/plan` prompt, or asked to "plan", you are strictly forbidden from using `write_file`, `patch_file`, or any tools that modify the repository.
- **Explicit Handover:** You must only provide a conceptual roadmap or update the `plan.md`.
- **Approval Gate:** Even if the user prompt implies action (e.g., "Plan and fix this"), you must respond: "The plan is ready. Please switch to Edit Mode and type 'execute' to begin implementation."
- **Tool Restriction:** Use `read_file`, `ls`, and `grep` to gather context, but wait for a mode shift before applying changes.

## Markdown Writing

When writing markdown files (README.md, AGENTS.md, CHANGELOG.md, PLAN.md, etc.), follow the [markdownlint rules](https://github.com/markdownlint/markdownlint/blob/main/docs/RULES.md) to maintain consistent and high-quality documentation.

### Rules Reference

For complete rule documentation, see: <https://github.com/markdownlint/markdownlint/blob/main/docs/RULES.md>

- **MD001 (heading-increment)**: Heading levels should only increment by one level at a time (no skipping from h1 to h3)
- **MD002 (first-heading-h1)**: First heading should be level 1
- **MD003 (heading-style)**: Heading style should be consistent (ATX, ATX_CLOSED, Setext, Setext_with_atx, consistent)
- **MD004 (ul-style)**: Unordered list style should be consistent
- **MD005 (list-indent)**: Inconsistent indentation for list items at the same level
- **MD006 (ul-start-left)**: Consider starting unordered lists at the beginning of the line
- **MD007 (ul-indent)**: Unordered list indentation should be incremented by 2 spaces
- **MD009 (no-trailing-spaces)**: Trailing spaces should be removed
- **MD010 (no-hard-tabs)**: Hard tabs should be replaced with spaces
- **MD011 (no-reversed-links)**: Reversed link syntax should not be used
- **MD012 (no-multiple-blanks)**: Multiple consecutive blank lines should be reduced to one
- **MD013 (line-length)**: Line length should not exceed a configured limit
- **MD014 (commands-show-output)**: Dashes should not be used instead of a colon for separating list item content
- **MD018 (no-missing-space-atx)**: No space after hash on atx style heading
- **MD019 (no-multiple-space-atx)**: Multiple spaces after hash on atx style heading
- **MD020 (no-missing-space-closed-atx)**: No space inside hashes on closed atx style heading
- **MD021 (no-multiple-space-closed-atx)**: Multiple spaces inside hashes on closed atx style heading
- **MD022 (blanks-around-headings)**: Headings should be surrounded by blank lines
- **MD023 (heading-starts-line)**: Headings must start at the beginning of the line
- **MD024 (no-duplicate-heading)**: Multiple headings with the same content
- **MD025 (single-h1)**: Multiple top level headings in the same document
- **MD026 (no-trailing-punctuation)**: Trailing punctuation in heading
- **MD027 (no-multiple-space-setext)**: Multiple spaces after blockquote symbol
- **MD028 (no-blanks-blockquote)**: Blank line inside blockquote
- **MD029 (ol-prefix)**: Ordered list item prefix
- **MD030 (list-marker-space)**: Spaces after list markers
- **MD031 (blanks-around-fences)**: Fenced code blocks should be surrounded by blank lines
- **MD032 (blanks-around-lists)**: Lists should be surrounded by blank lines
- **MD033 (no-inline-html)**: Inline HTML should not be used
- **MD034 (no-bare-urls)**: Bare URL used
- **MD035 (hr-style)**: Horizontal rule style should be consistent
- **MD036 (no-emphasis-as-heading)**: Emphasis used instead of a heading
- **MD037 (no-space-in-emphasis)**: Spaces inside emphasis markers
- **MD038 (no-space-in-code)**: Spaces inside code span elements
- **MD039 (no-space-in-links)**: Spaces inside link text
- **MD040 (fenced-code-language)**: Fenced code blocks should have a language specified
- **MD041 (first-line-heading)**: First line in file should be a top level heading
- **MD042 (no-empty-links)**: No empty links
- **MD043 (required-headings)**: Required heading structure
- **MD044 (proper-names)**: Proper names should have the correct capitalization
- **MD045 (no-alt-text)**: Images should have alternate text (alt text)
- **MD046 (code-block-style)**: Code block style should be consistent
- **MD047 (single-trailing-newline)**: Files should end with a single newline character
- **MD048 (code-fence-style)**: Code fence style should be consistent
- **MD049 (emphasis-style)**: Emphasis style should be consistent
- **MD050 (strong-style)**: Strong style should be consistent
- **MD051 (link-fragments)**: Link fragments should be valid

### Examples

❌ **WRONG**:

```markdown
#No Space After Hash
Some content

##  Too Much Space

Some content
- Item 1
- Item 2
No blank line after list

Next paragraph
```

✅ **CORRECT**:

```markdown
# Proper Heading

Some content

## Proper Subheading

Some content

- Item 1
- Item 2

Next paragraph
```

## F# Development Rules

### General Style

- **Follow the official [Microsoft F# Style Guide](https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/)** for all F# code in this repository.
- At the end of each task, format the code with the command `dotnet fantomas src tests`.

### Declaration Order

**CRITICAL**: In F#, declarations must appear in dependency order. A type/function must be declared BEFORE it is referenced.

#### Examples

❌ **WRONG** - Converter references `BookSearchDto` before it's declared:

```fsharp
type SkipInvalidBooksConverter() =
    inherit JsonConverter<BookSearchDto list>()  // Error! BookSearchDto not yet declared
    // ...

type BookSearchDto = { ... }
```

✅ **CORRECT** - `BookSearchDto` declared first:

```fsharp
type BookSearchDto = { ... }

type SkipInvalidBooksConverter() =
    inherit JsonConverter<BookSearchDto list>()  // OK - BookSearchDto already declared
    // ...
```

#### Rules

1. Always declare types/functions BEFORE using them
2. Order matters within modules - read top to bottom
3. Use `and` keyword for mutually recursive types if needed
4. Avoid using `rec module` unless strictly required, for instance when using `nameof` function with the module.

### F# Idioms

#### TryXxx Methods
When using BCL `TryXxx` methods that return `(bool * 'T)`, prefer pattern matching over checking the boolean result:

❌ **AVOID**:
```fsharp
let hasValue = dict.TryGetValue(key, &value)
if hasValue then
    // use value
```

✅ **PREFER**:
```fsharp
match dict.TryGetValue(key) with
| true, value -> // use value
| false, _ -> // handle missing case
```

Or for multiple conditions:
```fsharp
match obj.TryGetProperty("foo"), obj.TryGetProperty("bar") with
| (true, _), (true, _) -> // both present
| _ -> // at least one missing
```

### String Interpolation

Always specify the format specifier (e.g., `%i`, `%s`, `%f`, `%d`) in string interpolation to ensure values are formatted correctly and prevent accidentally formatting an entire object instead of a specific value:

❌ **AVOID** - Object gets formatted instead of its value:

```fsharp
let user = { Name = "Alice"; Age = 30 }
let message = $"User: {user}"  // Formats entire object, not individual properties
```

✅ **PREFER** - Explicit format specifiers:

```fsharp
let user = { Name = "Alice"; Age = 30 }
let message = $"User: {user.Name:s} (age {user.Age:i})"  // Clear, explicit formatting
```

## JSON Serialization

- The default serializer uses `JsonNamingPolicy.CamelCase`, not snake_case
- Snake_case API responses need `[<JsonPropertyName("snake_case")>]` attributes
- Custom `JsonConverter` can filter invalid entries during deserialization
