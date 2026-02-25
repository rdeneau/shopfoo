## Phase 4 Complete: Client Drawer & Actions Integration

Created the ReceiveSupply drawer form with date, quantity, and purchase price inputs following
the AdjustStock MVU pattern. Wired the action button in the "last-purchase-price" dropdown,
handled drawer close with data refresh, added translations (EN/FR), and ticked the README checkbox.

**Files created/changed:**

- `src\Shopfoo.Client\Pages\Product\Details\ReceiveSupply.fs` (new)
- `src\Shopfoo.Client\Shopfoo.Client.fsproj`
- `src\Shopfoo.Client\Pages\Product\Details\Page.fs`
- `src\Shopfoo.Client\Pages\Product\Details\Actions.fs`
- `src\Shopfoo.Shared\Translations.fs`
- `src\Shopfoo.Home\Data\Translations.fs`
- `README.md`

**Functions created/changed:**

- `ReceiveSupplyForm` React component (ReceiveSupply.fs)
- `ReceiveSupply.init` / `update` MVU functions (ReceiveSupply.fs)
- `ReceiveSupply.Model.CloseDrawer` with conditional drawer data (ReceiveSupply.fs)
- `RefreshAfterSupply` message handler (Actions.fs)
- Action button in "last-purchase-price" ActionsDropdown (Actions.fs)
- `ReceivePurchasedProducts` drawer close handler (Actions.fs)
- `ReceivePurchasedProducts` drawer rendering (Page.fs)

**Tests created/changed:**

- None (UI component; all 81 existing tests pass)

**Review Status:** APPROVED with minor fix applied (conditional CloseDrawer pattern)

**Git Commit Message:**

```txt
feat: ✨ Add Receive Purchased Products drawer

- Create ReceiveSupply drawer form with date, quantity, and price inputs
- Add action button in purchase price section dropdown
- Refresh purchase price stats and stock on drawer close after save
- Add French and English translations for form labels
- Tick README checkbox for completed feature

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
