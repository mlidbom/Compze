
---
applyTo: '**'
---

## Coding Rules

- **Exceptions**: Never swallow exceptions in a catch block without rethrowing.
- **Comments**: Prefer descriptive names over explanatory comments. Do not add `// Arrange`, `// Act`, `// Assert` comments in tests.
- **DevScripts output**: Success should be silent — only write output when something goes wrong.

## Pluggable Component Testing

- **Never** write one test per pluggable component. Use `[PCT]` attribute + `UniversalTestBase` base class — this automatically tests ALL enabled combinations (see `copilot-instructions.md` for the full pattern).
- Test methods take zero parameters; access the current combination via the static `TestEnv` class.

## Code Quality Gates

- Run the full test suite (`C-Test`). If fewer than 958 tests execute, treat it as a failure.
- If performance tests fail, rerun them. Repeated failures are NOT acceptable — do not report success unless all tests pass.

