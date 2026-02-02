# AGENTS.md

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
