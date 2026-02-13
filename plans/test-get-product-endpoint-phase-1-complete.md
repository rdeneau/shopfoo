## Phase 1 Complete: Test GetProduct with ISBN (Books)

Created comprehensive tests for GetProduct endpoint with ISBN-based products, covering both success and not-found scenarios. Both tests pass, following existing test patterns and using TUnit/Unquote frameworks.

**Files created/changed:**

- [tests/Shopfoo.Product.Tests/GetProductShould.fs](tests/Shopfoo.Product.Tests/GetProductShould.fs) - Created
- [tests/Shopfoo.Product.Tests/Shopfoo.Product.Tests.fsproj](tests/Shopfoo.Product.Tests/Shopfoo.Product.Tests.fsproj) - Modified

**Functions created/changed:**

- `GetProductShould` class - New test class
- `return Some Product when ISBN exists in repository` - Test method for success case
- `return None when ISBN not found in repository` - Test method for not-found case

**Tests created/changed:**

- `return Some Product when ISBN exists in repository` - Asserts product.SKU matches expected ISBN
- `return None when ISBN not found in repository` - Asserts result is None

**Review Status:** APPROVED with minor recommendations

**Git Commit Message:**

```
✅ test(product): add GetProduct endpoint tests for ISBN

Add comprehensive test coverage for GetProduct endpoint with ISBN-based
products (books). Tests verify both success scenario (product found) and
not-found scenario (product doesn't exist).

- Create GetProductShould test class following existing patterns
- Test success case: assert product.SKU matches expected ISBN
- Test not-found case: assert None returned
- Use ApiTestFixture with in-memory book repository
- Follow TUnit/Unquote testing conventions
```
