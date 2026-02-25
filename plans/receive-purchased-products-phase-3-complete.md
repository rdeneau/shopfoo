## Phase 3 Complete: Backend Wiring (Server Handler)

Wired the `ReceiveSupplyHandler` stub to actually call `api.Product.ReceiveSupply(input)`,
following the exact `AdjustStockHandler` pattern. All other backend wiring (pipeline, API,
builder, authorization) was already completed in previous phases.

**Files created/changed:**

- `src\Shopfoo.Server\Remoting\Prices\ReceiveSupplyHandler.fs`

**Functions created/changed:**

- `ReceiveSupplyHandler.Handle` — now calls API and maps result through ResponseBuilder

**Tests created/changed:**

- None (handler wiring verified by compilation and existing tests)

**Review Status:** APPROVED

**Git Commit Message:**

```txt
feat: 👔 Wire ReceiveSupply server handler

- Connect ReceiveSupplyHandler to api.Product.ReceiveSupply
- Map result through ResponseBuilder following AdjustStock pattern

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
