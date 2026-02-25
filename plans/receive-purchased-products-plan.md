## Plan: Receive Purchased Products Action

Add a "Receive purchased products" action to the product details page, including a drawer form
(date, quantity, purchase price), a backend workflow with validation guards, and full wiring across
all layers (shared types, server handler, client UI).

**Phases (4 phases)**

1. **Phase 1: Shared Types & API Contract**

    - **Objective:** Define the input type for the new command and extend the remoting API contract
    - **Files/Functions to Modify/Create:**
      - `src\Shopfoo.Shared\Remoting.fs` — Add `ReceiveSupplyInput` record and `ReceiveSupply` command to `PricesApi`
      - `src\Shopfoo.Client\Pages\Product\Shared.fs` — Update `Drawer.ReceivePurchasedProducts` to carry currency data
      - `src\Shopfoo.Shared\Translations.fs` — Add `ReceivePurchasedProducts` translation key to `StockAction`
    - **Tests to Write:** None (type definitions only; compilation is the test)
    - **Steps:**
      1. Add `ReceiveSupplyInput` record to `Remoting.fs` with fields: `SKU`, `Date: DateOnly`, `Quantity: int`, `PurchasePrice: Money`
      2. Add `ReceiveSupply: Command<ReceiveSupplyInput>` to the `PricesApi` record
      3. Update `Drawer.ReceivePurchasedProducts` in `Shared.fs` to carry `Currency` (so the drawer form knows the currency)
      4. Add `ReceivePurchasedProducts` translation key in `Translations.fs` under `StockAction`
      5. Fix all compilation errors caused by the `Drawer` case change (pattern matches in `Actions.fs` and `Page.fs`)
      6. Verify the solution compiles

2. **Phase 2: Backend Workflow with Validation (TDD)**

    - **Objective:** Create the `ReceiveSupply` workflow with guards (quantity > 0, price > 0) following TDD
    - **Files/Functions to Modify/Create:**
      - `src\Shopfoo.Product.Tests\Workflows\ReceiveSupplyShould.fs` — New test file
      - `src\Shopfoo.Product.Tests\Shopfoo.Product.Tests.fsproj` — Add new test file
      - `src\Shopfoo.Product\Workflows\Prelude.fs` — Add `AddStockEvent` to `IProductInstructions`
      - `src\Shopfoo.Product\Workflows\ReceiveSupply.fs` — New workflow
      - `src\Shopfoo.Product\Shopfoo.Product.fsproj` — Add new workflow file
    - **Tests to Write:**
      - `RejectSupply_WhenQuantityIsZeroOrNegative`
      - `RejectSupply_WhenPurchasePriceIsZeroOrNegative`
      - `CreateStockEvent_WhenInputIsValid`
    - **Steps:**
      1. Create `ReceiveSupplyShould.fs` with the three test cases listed above, following the `DetermineStockShould.fs` pattern with mock `IProductInstructions`
      2. Add the test file to the `.fsproj`
      3. Run tests — verify they fail (Red)
      4. Add `AddStockEvent` abstract member to `IProductInstructions` in `Prelude.fs`
      5. Create `ReceiveSupply.fs` workflow implementing `IProductWorkflow<ReceiveSupplyInput, unit>` with validation guards
      6. Add the workflow file to `Shopfoo.Product.fsproj`
      7. Run tests — verify they pass (Green)
      8. Refactor if needed

3. **Phase 3: Backend Wiring (Pipeline, API, Server Handler)**

    - **Objective:** Wire the workflow through the pipeline, product API, server handler, and API builder
    - **Files/Functions to Modify/Create:**
      - `src\Shopfoo.Product\Data\Warehouse.fs` — Add `ReceiveSupply` method to `WarehousePipeline`
      - `src\Shopfoo.Product\Api.fs` — Add `ReceiveSupply` to `IProductApi` interface and `Api` implementation
      - `src\Shopfoo.Server\Remoting\Prices\ReceiveSupplyHandler.fs` — New handler file
      - `src\Shopfoo.Server\Shopfoo.Server.fsproj` — Add new handler file
      - `src\Shopfoo.Server\Remoting\Prices\PricesApiBuilder.fs` — Wire handler with authorization
    - **Tests to Write:** None (integration wiring; existing tests + compilation verify correctness)
    - **Steps:**
      1. Add `ReceiveSupply` method to `WarehousePipeline`, creating a `StockEvent` with `ProductSupplyReceived` event type
      2. Add `ReceiveSupply` to `IProductApi` and implement in `Api` class, calling the workflow then pipeline
      3. Create `ReceiveSupplyHandler.fs` following the `AdjustStockHandler.fs` pattern
      4. Add the handler file to `Shopfoo.Server.fsproj`
      5. Wire `ReceiveSupply` in `PricesApiBuilder.Build()` with appropriate warehouse/stock claims
      6. Verify the solution compiles and all existing tests pass

4. **Phase 4: Client Drawer & Actions Integration**

    - **Objective:** Create the drawer form component and wire the action button in the product details page
    - **Files/Functions to Modify/Create:**
      - `src\Shopfoo.Client\Pages\Product\Details\ReceiveSupply.fs` — New drawer form component
      - `src\Shopfoo.Client\Shopfoo.Client.fsproj` — Add new file
      - `src\Shopfoo.Client\Pages\Product\Details\Page.fs` — Replace `"🚧 TODO"` with `ReceiveSupplyForm`
      - `src\Shopfoo.Client\Pages\Product\Details\Actions.fs` — Add action button to `ActionsDropdown` and handle drawer close
      - `README.md` — Tick the "Receive purchased products" checkbox
    - **Tests to Write:** None (UI component; manual verification)
    - **Steps:**
      1. Create `ReceiveSupply.fs` following the `AdjustStock.fs` MVU pattern with fields: Date (DateOnly), Quantity (int), Purchase Price (decimal) — using the currency from the drawer data
      2. Add the file to `Shopfoo.Client.fsproj` before `Page.fs`
      3. In `Page.fs`, replace the `"🚧 TODO"` for `ReceivePurchasedProducts` with the new `ReceiveSupplyForm` component
      4. In `Actions.fs`, add an action entry to the `ActionsDropdown "last-purchase-price"` (currently empty `[]`) that opens `Drawer.ReceivePurchasedProducts` with the current currency
      5. In `Actions.fs` `drawerControl.OnClose`, handle `ReceivePurchasedProducts` to refresh purchase price stats
      6. Tick the checkbox in `README.md` line 119
      7. Verify the solution compiles

**Open Questions**

1. Should the reception date default to today, or be left empty for the user to fill? Defaulting to today seems most practical.
2. Should quantity validation reject zero (only positive) or allow zero? Only positive (> 0) seems correct for a supply reception.
3. Should the purchase price validation use the product's existing currency, or allow selecting a different currency? Using the existing currency (from `Prices.Currency`) keeps it simple.
4. What authorization claim should gate this action — `Feat.Warehouse` with `Access.Edit`, or a separate purchasing claim? Using `Feat.Warehouse` + `Access.Edit` matches the existing stock operations pattern.
