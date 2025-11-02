
---
applyTo: '**'
---

- DO NOT 
  - Swallow exceptions in a catch block that does not rethrow
  - Sprinkle explanatory comments everywhere. Use descriptive names instead.
- - Add // Arrange etc comments in tests. The developers know how to write tests.
  - Write one test per pluggable component, instead use the DuplicateByPluggableComponentTest structure so that they are ALL tested automatically, including future versions
  - Write log spam in DevScripts PowerShell functions. Scripts that make changes should only write output if something goes wrong. Success should be silent.
- DO
  - Use descriptive variable and method names. Long names are fine if they make the code clearer.
  - Use InternalsVisibleTo to maintain encapsulation within framework code
  - Run the FULL test suite to ensure no tests are broken.
    - If less than 958 tests are executed something has gone wrong and it counts as test failure.
  - Test pluggable components (DI containers and persistence layers) using the DuplicateByPluggableComponentTest structure.
- IF
  - Performance tests fail
    - Rerun them. REPEATED failures is NOT OK. Do not report success unless all tests pass.

