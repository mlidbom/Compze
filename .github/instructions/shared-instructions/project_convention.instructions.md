---
applyTo: '**'
---

## Coding Rules

- **Exceptions**: Never swallow exceptions in a catch block without rethrowing.
- **Comments**: Prefer descriptive names over explanatory comments. Do not add `// Arrange`, `// Act`, `// Assert` comments in tests.
- **DevScripts output**: Success should be silent — only write output when something goes wrong.

## Code Quality

- If performance tests fail, rerun them. Repeated failures are NOT acceptable — do not report success unless all tests pass.