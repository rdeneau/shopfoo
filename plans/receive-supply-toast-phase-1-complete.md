## Phase 1 Complete: Wire Toast Notification

Added success/error toast notification after saving in the "Receive purchased products" drawer,
following the same callback pattern used by AdjustStock and ManagePrice drawers.

**Files changed:**

- `src/Shopfoo.Client/Pages/Shared.fs`
- `src/Shopfoo.Client/Pages/Product/Details/ReceiveSupply.fs`
- `src/Shopfoo.Client/Pages/Product/Details/Page.fs`
- `src/Shopfoo.Client/View.fs`

**Functions created/changed:**

- `Toast.Supply` case added to `Toast` DU
- `ReceiveSupplyForm` — added `onSave` callback parameter
- `update` in ReceiveSupply — fires `onSave` via `Cmd.ofEffect` on `Done`
- `onSaveSupply` callback in Page.fs
- Toast rendering case for `Toast.Supply` in View.fs

**Tests created/changed:**

- None (UI wiring only, no domain logic)

**Review Status:** APPROVED

**Git Commit Message:**

```text
feat: 🚸 Add toast notification on receive supply save

- Add Supply case to Toast discriminated union
- Wire onSave callback in ReceiveSupplyForm
- Render success/error toast with stock label and SKU

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
