## Plan: Test GetProduct Endpoint

Create comprehensive tests for the existing GetProduct endpoint in Shopfoo.Product Api, covering all SKU types (ISBN, FSID, OLID, Unknown) and both success and not-found scenarios. Since production code already exists, tests will verify existing behavior rather than drive new implementation.

**Phases: 5**

1. **Phase 1: Test GetProduct with ISBN (Books)**

   - **Objective:** Test GetProduct endpoint for ISBN-based products (books), covering both success and not-found scenarios
   - **Files/Functions to Modify/Create:**
     - Create [tests/Shopfoo.Product.Tests/GetProductShould.fs](tests/Shopfoo.Product.Tests/GetProductShould.fs)
     - Update [tests/Shopfoo.Product.Tests/Shopfoo.Product.Tests.fsproj](tests/Shopfoo.Product.Tests/Shopfoo.Product.Tests.fsproj) to include new test file
   - **Tests to Write:**
     - `return Some Product when ISBN exists in repository` - Assert that returned product has expected ISBN
     - `return None when ISBN not found in repository`
   - **Steps:**
     1. Write test for getting existing book by ISBN, asserting product.Id matches expected ISBN
     2. Run test to verify it passes (green) - production code already exists
     3. Write test for ISBN not found scenario, asserting result is None
     4. Run test to verify it passes (green)
     5. Refactor tests if needed for clarity
     6. Run linter/formatter (fantomas)

2. **Phase 2: Test GetProduct with FSID (FakeStore/Bazaar)**

   - **Objective:** Test GetProduct endpoint for FSID-based products from FakeStore, covering both success and not-found scenarios
   - **Files/Functions to Modify/Create:**
     - Update [tests/Shopfoo.Product.Tests/GetProductShould.fs](tests/Shopfoo.Product.Tests/GetProductShould.fs)
   - **Tests to Write:**
     - `return Some Product when FSID exists in FakeStore` - Assert that returned product has expected FSID
     - `return None when FSID not found in FakeStore`
   - **Steps:**
     1. Write test for getting existing FakeStore product by FSID with mocked IFakeStoreClient (using NSubstitute)
     2. Configure ApiTestFixture with FakeStore mock returning product
     3. Run test to verify it passes (green), asserting product.Id matches expected FSID
     4. Write test for FSID not found scenario
     5. Configure NSubstitute mock to return None for not-found scenario
     6. Run test to verify it passes (green), asserting result is None
     7. Refactor tests and fixture setup for clarity
     8. Run linter/formatter (fantomas)

3. **Phase 3: Test GetProduct with OLID (OpenLibrary)**

   - **Objective:** Test GetProduct endpoint for OLID-based products from OpenLibrary, covering both success and not-found scenarios
   - **Files/Functions to Modify/Create:**
     - Update [tests/Shopfoo.Product.Tests/GetProductShould.fs](tests/Shopfoo.Product.Tests/GetProductShould.fs)
   - **Tests to Write:**
     - `return Some Product when OLID exists in OpenLibrary` - Assert that returned product has expected OLID
     - `return None when OLID not found in OpenLibrary`
   - **Steps:**
     1. Write test for getting existing OpenLibrary product by OLID with mocked IOpenLibraryClient (using NSubstitute)
     2. Configure ApiTestFixture with OpenLibrary mock returning product
     3. Run test to verify it passes (green), asserting product.Id matches expected OLID
     4. Write test for OLID not found scenario
     5. Configure NSubstitute mock to return None for not-found scenario
     6. Run test to verify it passes (green), asserting result is None
     7. Refactor tests and fixture setup for clarity
     8. Run linter/formatter (fantomas)

4. **Phase 4: Test GetProduct with Unknown SKU**

   - **Objective:** Test GetProduct endpoint behavior when called with an Unknown SKU type
   - **Files/Functions to Modify/Create:**
     - Update [tests/Shopfoo.Product.Tests/GetProductShould.fs](tests/Shopfoo.Product.Tests/GetProductShould.fs)
   - **Tests to Write:**
     - `return None for Unknown SKU type`
   - **Steps:**
     1. Write test for Unknown SKU type
     2. Run test to verify it passes (green) - existing production code handles this
     3. Refactor test for clarity if needed
     4. Run linter/formatter (fantomas)

5. **Phase 5: Verify Complete Test Coverage**

   - **Objective:** Ensure all tests pass together and verify comprehensive coverage of GetProduct endpoint
   - **Files/Functions to Modify/Create:**
     - Review/refactor [tests/Shopfoo.Product.Tests/GetProductShould.fs](tests/Shopfoo.Product.Tests/GetProductShould.fs)
   - **Tests to Write:**
     - No new tests, verify all existing tests
   - **Steps:**
     1. Run all GetProduct tests together
     2. Verify 7 tests pass (2 ISBN + 2 FSID + 2 OLID + 1 Unknown)
     3. Review test code for duplication and refactor if needed
     4. Verify tests follow existing patterns in codebase
     5. Run linter/formatter (fantomas) on final code
     6. Confirm no regression in other tests

**Testing Approach:**

- Use example-based tests (no property-based testing with FsCheck needed)
- Assert only that the returned product has the expected Id (SKU)
- Use NSubstitute for mocking external API clients (IFakeStoreClient, IOpenLibraryClient)
- Focus on functional correctness (no performance or concurrency tests)
