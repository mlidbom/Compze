---
paths:
  - "test/**/*.cs"
---

# Compze-Specific Test Conventions

For full BDD rationale and examples, see [src/Compze.xUnitBDD/README.md](../../../src/Compze.xUnitBDD/README.md).

## Test Attributes (local test infrastructure)

| Attribute | Purpose |
| --- | --- |
| `[PCT]` | Pluggable Component Theory — runs the test for every configured component combination (SqlLayer × DIContainer × Serializer × Transport). |
| `[PCTSerializer]` | Varies only the Serializer component. |
| `[Performance]` | Marks performance tests. |
| `[LongRunning]` | Marks long-running tests. |

## Pluggable Component Testing

- **Never write one test per pluggable component.** Use `[PCT]` + `UniversalTestBase` — it automatically
  tests all enabled combinations.
- Test methods take zero parameters; access the current combination via the static `TestEnv` class.
- **Anything reading `TestEnv`'s pluggable components (`TestEnv.DIContainer`, `TestingEndpointHost.Create`,
  `CreateTestingContainerBuilder`, …) MUST run under a `[PCT*]` attribute** — the components context exists
  only inside matrix-driven test cases. An `[XF]`/`[Fact]` test touching them fails at the test-class
  constructor with "No components provider found any components".

## Base Class

- **Inherit from `UniversalTestBase`** when tests need lifecycle management — it provides `IDisposable` and `IAsyncLifetime` via protected virtual overrides.
- Override these protected virtual methods instead of implementing `IDisposable`/`IAsyncLifetime` directly:
  - `DisposeInternal()` — synchronous cleanup.
  - `InitializeAsyncInternal()` — async initialization (e.g., `await Host.StartAsync()`).
  - `DisposeAsyncInternal()` — async cleanup (e.g., `await Host.DisposeAsync()`).

## Integration Tests

- Create a `TestingEndpointHost`, register endpoints, start/stop in lifecycle methods:
  ```csharp
  protected override async Task InitializeAsyncInternal() => await _host.StartAsync();
  protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();
  ```
- Use `IServiceLocator` for resolving services: `ServiceLocator.ExecuteTransactionInIsolatedScope(...)`.
- Use `IThreadGate` for controlling concurrency timing: `gate.Close()`, `gate.Open()`, `gate.AwaitPassedThroughCountEqualTo(n)`.

## Performance Tests

- Performance tests live in `Compze.Tests.Performance.Internals`.
- Use `TimeAsserter.Execute(action, iterations: N, maxTotal: duration)` for timed assertions.
- Use `EnvDivide()` / `EnvMultiply()` on thresholds to adjust for slow/instrumented machines.
- Warm up with `StopwatchCE.TimeExecution()` before measuring.
- Mark performance test projects with `[assembly: PerformanceAttribute]`.
- Test parallelization is disabled in performance test projects.

## Code Policy Tests

- Code policy tests live in `Compze.Tests.CodePolicies`.
- Typically static classes with `[XF]` methods that scan assemblies for violations.
