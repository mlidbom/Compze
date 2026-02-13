---
applyTo: '**'
---

## Compze-Specific Conventions


### Code Quality Gates

- If performance tests fail, rerun them. Repeated failures are NOT acceptable — do not report success unless all tests pass.
- Run the full test suite (`C-Test`). All the tests should succeed, if not treat it as a failure.
  - At current there are about 990 tests in the solution. If a test run contains significantly less, it is a failure, something has gone wrong. Test count should not decrease.