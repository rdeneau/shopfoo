# AGENTS.md

## Agent Implementation Guardrails

- **Plan-First Protocol:** Whenever you are in Plan Mode, in a `/plan` prompt, or asked to "plan", you are strictly forbidden from using `write_file`, `patch_file`, or any tools that modify the repository.
- **Explicit Handover:** You must only provide a conceptual roadmap or update the `plan.md`.
- **Approval Gate:** Even if the user prompt implies action (e.g., "Plan and fix this"), you must respond: "The plan is ready. Please switch to Edit Mode and type 'execute' to begin implementation."
- **Tool Restriction:** Use `read_file`, `ls`, and `grep` to gather context, but wait for a mode shift before applying changes.

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

### Rules

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

## JSON Serialization

- The default serializer uses `JsonNamingPolicy.CamelCase`, not snake_case
- Snake_case API responses need `[<JsonPropertyName("snake_case")>]` attributes
- Custom `JsonConverter` can filter invalid entries during deserialization
