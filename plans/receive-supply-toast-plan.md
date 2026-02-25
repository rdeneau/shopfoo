## Plan: Add Toast on Receive Supply Save

Add a success/error toast notification after saving in the "Receive purchased products" drawer,
following the same callback pattern used by AdjustStock and ManagePrice drawers.

**Phases: 1**

1. **Phase 1: Wire Toast Notification**

    - **Objective:** Add toast feedback on save for ReceiveSupply drawer, consistent with AdjustStock UX
    - **Files/Functions to Modify/Create:**
      - `src/Shopfoo.Client/Pages/Shared.fs` — Add `Supply` case to `Toast` DU
      - `src/Shopfoo.Client/Pages/Product/Details/ReceiveSupply.fs` — Add `onSave` callback, fire in `Done` handler
      - `src/Shopfoo.Client/Pages/Product/Details/Page.fs` — Wire `onSaveSupply` callback with `env.ShowToast`
      - `src/Shopfoo.Client/View.fs` — Add `Toast.Supply` rendering case
    - **Tests to Write:** No new tests (UI wiring only, no domain logic)
    - **Steps:**
      1. Add `| Supply of SKU * ApiError option` to the `Toast` DU in `Shared.fs`
      2. Add `onSave: SKU * ApiError option -> unit` parameter to `ReceiveSupplyForm` and pass it through to `update`
      3. In `update`, on `ReceiveSupply(Done result)`, add `Cmd.ofEffect` calling `onSave(model.SKU, result |> Result.tryGetError)`
      4. In `Page.fs`, create `onSaveSupply` callback calling `env.ShowToast(Toast.Supply(sku, error))` and pass it to `ReceiveSupplyForm`
      5. In `View.fs`, add pattern match for `Toast.Supply(sku, error)` rendering `toast` with `"Stock {SKU}"` label
      6. Verify build succeeds
