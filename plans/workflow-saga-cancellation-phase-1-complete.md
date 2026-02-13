## Phase 1 Complete: Add Error and Status Domain Types

Extended domain types to support workflow cancellation by adding `WorkflowCancelled` error case and `Cancelled` saga status. All pattern matches updated and 2 new unit tests verify the types work correctly.

**Files created/changed:**

- src/Shopfoo.Domain.Types/Errors.fs
- src/Shopfoo.Program/Saga.fs
- src/Shopfoo.Shared/Errors.fs
- tests/Shopfoo.Program.Tests/SagaShould.fs
- tests/Shopfoo.Program.Tests/Shopfoo.Program.Tests.fsproj

**Functions created/changed:**

- Error type - added `WorkflowCancelled of string` case
- SagaStatus type - added `Cancelled` case
- ErrorCategory.ofError - added WorkflowCancelled pattern match
- ErrorMessage.ofError - added WorkflowCancelled pattern match
- ApiError.FromError - added WorkflowCancelled pattern match

**Tests created/changed:**

- SagaShould.``Create WorkflowCancelled error with a message``
- SagaShould.``Pattern match SagaStatus.Cancelled``

**Review Status:** APPROVED

**Git Commit Message:**

```
feat: ✨ Add WorkflowCancelled error and Cancelled saga status

- Add WorkflowCancelled of string case to Error domain type
- Add Cancelled case to SagaStatus type
- Update all pattern matches (ErrorCategory, ErrorMessage, ApiError)
```
