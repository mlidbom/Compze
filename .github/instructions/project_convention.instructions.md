
---
applyTo: '**'
---

## Compze-Specific Conventions

### Namespace Examples

- **Namespace = folder path**: `Compze.Tests.Unit.MyFeature` must live under the `Compze.Tests.Unit` project in a `MyFeature/` subdirectory.

### Code Quality Gates

- Run the full test suite (`C-Test`). If fewer than 958 tests execute, treat it as a failure.

