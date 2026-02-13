---
applyTo: '**'
---

## Coding Rules

- **Exceptions**: Never swallow exceptions in a catch block without rethrowing.
- **Comments**: Prefer descriptive names over explanatory comments. Do not add `// Arrange`, `// Act`, `// Assert` comments in tests.

## Code Quality

- If performance tests fail, rerun them. Repeated failures are NOT acceptable — do not report success unless all tests pass.

### Namespace Examples

- **Namespace = folder path**: `Compze.Tests.Unit.MyFeature` must live under the `Compze.Tests.Unit` project in a `MyFeature/` subdirectory.