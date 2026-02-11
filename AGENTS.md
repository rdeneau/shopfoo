# AGENTS.md

**TODO:** voir ce qu'il y a de bon à prendre ici (fait par Clément) :
https://avp-gitlab.availpro.com/GDS/gdsconnectivity/-/blob/master/AGENTS.md

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

#### Rules

1. Always declare types/functions BEFORE using them
2. Order matters within modules - read top to bottom
3. **Declare types in order without the `and` keyword whenever possible** - only use `and` for mutually recursive types
4. Avoid using `rec module` unless strictly required, for instance when using `nameof` function with the module

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

❌ **WRONG** - Using `and` when it's not necessary for recursion:

```fsharp
type Container and Item = { name: string }
```

✅ **CORRECT** - Separate declarations in order:

```fsharp
type Item = { name: string }
type Container = { items: Item list }
```

### Interface Definitions

**Always use the `[<Interface>]` attribute** on interfaces to prevent accidental conversion to abstract classes. The `IXxx` naming convention alone is not sufficient.

**Exception**: Pure marker interfaces defined with `interface end` syntax don't require the attribute.

❌ **WRONG** - Missing attribute:

```fsharp
type ILogger =
    abstract member Log: string -> unit
```

✅ **CORRECT** - With attribute:

```fsharp
[<Interface>]
type ILogger =
    abstract member Log: string -> unit
```

✅ **CORRECT** - Pure marker interface without attribute:

```fsharp
type IMarker = interface end
```

### F# Idioms

#### Class Member "this" Identifier

When a class member does not use the `this` identifier, discard it with an underscore (`_`):

❌ **AVOID** - Unused `this` identifier:

```fsharp
type Counter() =
    member this.Increment() =
        // this is not used
        42
```

✅ **CORRECT** - Discard with underscore:

```fsharp
type Counter() =
    member _.Increment() =
        // this identifier is not used
        42
```

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

## Product.Tests Patterns

### Test Structure and Naming

All behavior tests in `Shopfoo.Product.Tests` follow a consistent pattern inspired by behavior-driven development (BDD):

**Test Class Naming**: Follow the pattern `[Workflow]Should.fs` where workflow is the main operation being tested:

- `AddProductShould.fs` - Tests for adding products
- `DetermineStockShould.fs` - Tests for stock determination
- `MarkAsSoldOutShould.fs` - Tests for marking products as sold out

**Test Method Naming**: Use the backtick syntax to write test names as readable sentences starting with `[Workflow]Should.fs`:

```fsharp
member this.``do something given conditions`` () = ...
```

Examples for `MarkAsSoldOutShould`:

- `member _.``be rejected given a stock quantity greater than zero`` (stock: int) = ...`
- `member _.``update retail price to SoldOut given a product with no stock`` () = ...`

### Parameterized Tests

Use the `[<Arguments>]` attribute for simple input values to test multiple scenarios:

```fsharp
[<Test>]
[<Arguments(1)>]
[<Arguments(5)>]
[<Arguments(100)>]
member this.``test name`` (stock: int) =
    async {
        // Test implementation
    }
```

This creates separate test cases for stock values 1, 5, and 100, useful for boundary testing.

### Assertions with Unquote

All assertions use Swensen.Unquote's `=!` operator for readable, expressive assertions.

**Asserting on whole Result/Option types**: Always assert on the entire Result or Option, rather than pattern matching:

```fsharp
open Swensen.Unquote

// ❌ Avoid: Pattern matching followed by assertions
match result with
| Ok value -> value =! expected  // Don't do this
| _ -> ()

// ✅ Correct: Assert on the whole Result
result =! Ok expectedValue
result =! Error(DataError(DataNotFound("SKU", "Prices")))

// ✅ Correct: Assert on whole Option
maybeValue =! Some expectedValue
maybeValue =! None

// ✅ Correct: Assert on the whole error object (F# record) for better clarity
let expectedError = { EntityName = "Stock"; ErrorMessage = "Stock quantity must be zero to mark as sold out." }
result =! Error(Error.Validation [ expectedError ])

// ❌ Avoid: Checking only parts of the error (C#-like assertions)
match result with
| Error(Error.Validation errors) ->
    errors
    |> List.exists (fun err -> err.ErrorMessage.Contains("expected text"))  // Don't do this
| _ -> false
```

The `=!` operator provides clear failure messages showing both expected and actual values, making test failures easy to debug.

### Avoiding Useless Comments in Tests

Test method names should be descriptive enough that comments are unnecessary. **Do not add comments that merely repeat what the test name already states.**

❌ **WRONG** - Comment duplicates the test name:

```fsharp
[<Test>]
member _.``succeed when RemoveListPrice is called with existing prices`` () =
    async {
        // Test: RemoveListPrice succeeds with valid prices
        let isbn = ISBN "978-0-13-468599-6"

        // Verify result is OK
        let! result = fixture.Api.RemoveListPrice sku
        result =! Ok()
    }
```

✅ **CORRECT** - Let the test name speak for itself:

```fsharp
[<Test>]
member _.``succeed when RemoveListPrice is called with existing prices`` () =
    async {
        let isbn = ISBN "978-0-13-468599-6"
        let sku = isbn.AsSKU
        let prices = Sales.Prices.Create(isbn, EUR, 19.99m, 25.99m)

        use fixture = new ApiTestFixture(pricesSet = [ prices ])
        let! result = fixture.Api.RemoveListPrice sku
        result =! Ok()
    }
```

**Exception:** Comments are allowed only when explaining non-obvious logic or test setup:

```fsharp
[<Test>]
member _.``fail when SavePrices fails after AddProduct`` () =
    async {
        let product = createValidBookProduct()
        use fixture = new ApiTestFixture()

        // First add the product successfully
        let! addResult = fixture.Api.AddProduct(product, EUR)
        addResult =! Ok()

        // Then attempt to save invalid prices - should fail
        let invalidPrices = { prices with RetailPrice = RetailPrice.Regular(Euros -1m) }
        let! savePricesResult = fixture.Api.SavePrices invalidPrices
        test <@ Result.isError savePricesResult @>
    }
```

### Avoiding Test Duplication

**Do not duplicate tests across test classes.** Each test class should test a specific aspect of the workflow:

- `AddProductShould.fs` - Tests adding products (validation, field requirements, etc.)
- `MarkAsSoldOutShould.fs` - Tests marking products as sold out (stock checks, price updates, etc.)
- `SavePricesShould.fs` - Tests saving price changes (validation, price constraints, etc.)
- `SagaUndoTests.fs` - Tests saga compensation behavior (undo when steps fail)

❌ **WRONG** - Duplicating an existing test:

```fsharp
// This test already exists in MarkAsSoldOutShould.fs
type SagaUndoTests() =
    [<Test>]
    member _.``succeed when MarkAsSoldOut is called with existing prices`` () =
        async {
            // ... same test as MarkAsSoldOutShould ...
        }
```

✅ **CORRECT** - New test class focuses on unique scenarios:

```fsharp
// SagaUndoTests tests multi-step workflows where later steps fail
type SagaUndoTests() =
    [<Test>]
    member _.``fail when SavePrices fails after AddProduct`` () =
        async {
            // Tests saga compensation: AddProduct succeeds,
            // then SavePrices fails, expecting AddProduct to be undone
            let product = createValidBookProduct()
            use fixture = new ApiTestFixture()

            let! addResult = fixture.Api.AddProduct(product, EUR)
            addResult =! Ok()

            let invalidPrices = { prices with RetailPrice = RetailPrice.Regular(Euros -1m) }
            let! savePricesResult = fixture.Api.SavePrices invalidPrices
            test <@ Result.isError savePricesResult @>

            // Verify the product was undone
            let! finalProduct = fixture.Api.GetProduct product.SKU
            finalProduct =! None
        }
```

**Guidelines for related test classes:**

1. **Identify the unique aspect** - What does this test class focus on that others don't?
2. **Skip obvious cases** - Don't re-test what existing classes already cover
3. **Test complementary scenarios** - Add tests that demonstrate the unique behavior (e.g., saga undo)
4. **Multi-step workflows** - Focus on interactions between operations that other classes test in isolation


### Test Fixture Pattern

Tests use `ApiTestFixture` to manage the dependency injection container lifecycle:

```fsharp
use fixture = new ApiTestFixture()
let! result = fixture.Api.SomeMethod()
```

For data-driven tests, pass optional arguments to populate the repositories:

```fsharp
use fixture =
    new ApiTestFixture(
        stockEvents = isbn.Events [ 5 |> Units.Purchased (Euros 24.99m) (365 |> daysAgo) ]
    )
```

The fixture ensures:

- Clean service provider per test
- Production-like DI configuration
- Mock external clients (FakeStore, OpenLibrary)
- Optional Sale and StockEvent repositories for test data
