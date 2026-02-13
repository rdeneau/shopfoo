# Plan: WorkflowSaga Cancellation Support

A new `WorkflowCancelled` error type will allow workflows to stop gracefully without triggering undo operations. The saga runner will recognize this special error and set a new `Cancelled` status instead of `Failed`, enabling clean workflow interruption using the result-based computation expression pattern.

**Phases (6 phases)**

1. **Phase 1: Add Error and Status Domain Types**
   - **Objective:** Extend domain types to support workflow cancellation
   - **Files/Functions to Modify/Create:**
     - [src/Shopfoo.Domain.Types/Errors.fs](src/Shopfoo.Domain.Types/Errors.fs) - Add `WorkflowCancelled of string` case to Error type
     - [src/Shopfoo.Program/Saga.fs](src/Shopfoo.Program/Saga.fs) - Add `Cancelled` case to SagaStatus type
   - **Tests to Write:**
     - Test that WorkflowCancelled error can be created with a message
     - Test that SagaStatus.Cancelled can be pattern matched
   - **Steps:**
     1. Write tests in [tests/Shopfoo.Program.Tests/SagaShould.fs](tests/Shopfoo.Program.Tests/SagaShould.fs) for new types
     2. Run tests to see them fail (types don't exist yet)
     3. Add `WorkflowCancelled of string` case to Error type in Errors.fs
     4. Add `Cancelled` case to SagaStatus type in Saga.fs
     5. Run tests to confirm they pass
     6. Lint and format code

2. **Phase 2: First Cancellation Test**
   - **Objective:** Write the first test for workflow cancellation after createOrder step
   - **Files/Functions to Modify/Create:**
     - [tests/Shopfoo.Program.Tests/OrderWorkflowSagaShould.fs](tests/Shopfoo.Program.Tests/OrderWorkflowSagaShould.fs) - Add ``cancel without undo: 1_ after createOrder`` test
   - **Tests to Write:**
     - ``cancel without undo: 1_ after createOrder`` - verifies cancellation behavior
   - **Steps:**
     1. Add the test exactly as shown in the template (lines 137-152)
     2. Run test to see it fail (WorkflowCancelled not returned, Cancelled status not set)
     3. Test should fail with appropriate error message

3. **Phase 3: Update cancelAfter to Return WorkflowCancelled**
   - **Objective:** Modify cancelAfter function to return WorkflowCancelled error after successful transition
   - **Files/Functions to Modify/Create:**
     - [tests/Shopfoo.Program.Tests/OrderContext/OrderWorkflows.fs](tests/Shopfoo.Program.Tests/OrderContext/OrderWorkflows.fs) - Update `cancelAfter` function
   - **Tests to Write:** (Already written in Phase 2)
   - **Steps:**
     1. Update cancelAfter to return `Error(WorkflowCancelled instructionName)` after transition
     2. Extract instruction name from transition (format: "TransitionOrderFrom{from}To{to}")
     3. Run test to see if it progresses (should still fail on Runner not handling Cancelled status)

4. **Phase 4: Update Runner to Handle Cancellation**
   - **Objective:** Modify Runner to detect WorkflowCancelled and set Cancelled status without undo
   - **Files/Functions to Modify/Create:**
     - [src/Shopfoo.Program/Runner.fs](src/Shopfoo.Program/Runner.fs) - Update `runWithCanUndo` function
   - **Tests to Write:** (Already written in Phase 2)
   - **Steps:**
     1. In runWithCanUndo, pattern match on Error to check for WorkflowCancelled
     2. When WorkflowCancelled detected, skip undo and set sagaState.Status to Cancelled
     3. Run test from Phase 2 to confirm it passes
     4. Lint and format code

5. **Phase 5: Refactor Test and Add Remaining Cancellation Points**
   - **Objective:** Extract VerifyCancel helper and add tests for all cancellation points in workflow
   - **Files/Functions to Modify/Create:**
     - [tests/Shopfoo.Program.Tests/OrderWorkflowSagaShould.fs](tests/Shopfoo.Program.Tests/OrderWorkflowSagaShould.fs) - Extract helper, add more tests
   - **Tests to Write:**
     - ``cancel without undo: 2_ after payOrder``
     - ``cancel without undo: 3_ after issueInvoice``
     - ``cancel without undo: 4_ after shipOrder``
   - **Steps:**
     1. Extract VerifyCancel helper method from first test
     2. Refactor first test to use VerifyCancel
     3. Run refactored test to ensure it still passes
     4. Write tests for remaining cancellation points using VerifyCancel
     5. Run all new tests to confirm they pass
     6. Lint and format code

6. **Phase 6: Update Error Presentation (No Tests)**
   - **Objective:** Add error message and categorization for WorkflowCancelled for debugging purposes
   - **Files/Functions to Modify/Create:**
     - [src/Shopfoo.Domain.Types/Errors.fs](src/Shopfoo.Domain.Types/Errors.fs) - Update `ErrorMessage.ofError` and `ErrorCategory.ofError`
   - **Tests to Write:** None - debugging-only changes
   - **Steps:**
     1. Add WorkflowCancelled case to ErrorMessage.ofError function (simple message with instruction name)
     2. Add WorkflowCancelled case to ErrorCategory.ofError function (categorize as TechnicalError)
     3. Lint and format code

**Implementation Notes**

- WorkflowCancelled is a technical error (not user or business error)
- Error message contains only the instruction name, no additional context
- Current instruction monitoring is sufficient, no special logging/metrics needed
