---
applyTo: '**'
---
## Code Organization
- Use InternalsVisibleTo to maintain encapsulation within framework code

## Standard instructions
- NEVER NEVER NEVER swallow exceptions in a catch block that does not rethrow
- After completing a code change, always run the full test suite to ensure no tests are broken.
  - If less than 715 tests are executed something has gone wrong and it counts as test failure.
