## Phase 1 Complete: Shared Types & API Contract

Defined `ReceiveSupplyInput` record type, extended `PricesApi` with `ReceiveSupply` command,
updated `Drawer.ReceivePurchasedProducts` to carry `Currency`, added translation key, created
stub server handler, and fixed all compilation errors.

**Files created/changed:**

- `src\Shopfoo.Shared\Remoting.fs`
- `src\Shopfoo.Client\Pages\Product\Shared.fs`
- `src\Shopfoo.Shared\Translations.fs`
- `src\Shopfoo.Client\Pages\Product\Details\Actions.fs`
- `src\Shopfoo.Client\Pages\Product\Details\Page.fs`
- `src\Shopfoo.Server\Remoting\Prices\ReceiveSupplyHandler.fs` (new)
- `src\Shopfoo.Server\Remoting\Prices\PricesApiBuilder.fs`
- `src\Shopfoo.Server\Shopfoo.Server.fsproj`

**Functions created/changed:**

- `ReceiveSupplyInput` record type (Remoting.fs)
- `PricesApi.ReceiveSupply` field (Remoting.fs)
- `Drawer.ReceivePurchasedProducts of Currency` (Shared.fs)
- `StockAction.ReceivePurchasedProducts` translation key (Translations.fs)
- `ReceiveSupplyHandler.Handle` stub (ReceiveSupplyHandler.fs)
- `PricesApiBuilder.Build` updated with ReceiveSupply wiring (PricesApiBuilder.fs)

**Tests created/changed:**

- None (type definitions only; compilation is the test)

**Review Status:** APPROVED

**Git Commit Message:**

```txt
feat: ✨ Add ReceiveSupply types and API contract

- Add ReceiveSupplyInput record with SKU, Date, Quantity, PurchasePrice
- Add ReceiveSupply command to PricesApi remoting contract
- Update Drawer.ReceivePurchasedProducts to carry Currency data
- Add ReceivePurchasedProducts translation key to StockAction
- Create stub ReceiveSupplyHandler with warehouse edit authorization
- Fix all pattern matches for updated Drawer case

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
