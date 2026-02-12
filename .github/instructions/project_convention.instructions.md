
---
applyTo: '**'
---

## Coding Rules

- **Exceptions**: Never swallow exceptions in a catch block without rethrowing.
- **Comments**: Prefer descriptive names over explanatory comments. Do not add `// Arrange`, `// Act`, `// Assert` comments in tests.
- **DevScripts output**: Success should be silent — only write output when something goes wrong.

## C# Style Preferences

- **File-scoped namespaces**: Always use `namespace Foo.Bar;` (not block-scoped).
- **Expression bodies**: Prefer `=>` syntax for single-expression methods, properties, and operators.
- **`var`**: Use `var` whenever the type is apparent or unimportant. Avoid explicit types unless needed for clarity.
- **Primary constructors**: Use when appropriate (see `PCTAttribute` for an example).
- **Indentation**: 3 spaces (match existing codebase).
- **Test method names**: Use underscores for readability (e.g., `My_test_method()`).
- **Namespace = folder path**: `Compze.Tests.Unit.MyFeature` must live in `Tests/Unit/MyFeature/`.

## Pluggable Component Testing

- **Never** write one test per pluggable component. Use `[PCT]` attribute + `UniversalTestBase` base class — this automatically tests ALL enabled combinations (see `copilot-instructions.md` for the full pattern).
- Test methods take zero parameters; access the current combination via the static `TestEnv` class.

## Code Quality Gates

- Run the full test suite (`C-Test`). If fewer than 958 tests execute, treat it as a failure.
- If performance tests fail, rerun them. Repeated failures are NOT acceptable — do not report success unless all tests pass.

