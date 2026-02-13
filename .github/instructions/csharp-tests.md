---
applyTo: "test/**/*.cs"
---

# Compze-Specific Test Conventions

For full BDD rationale and examples, see [src/Compze.Utilities.Testing.XUnit/README.md](../../src/Compze.Utilities.Testing.XUnit/README.md).

- **Inherit from `UniversalTestBase`** when tests need lifecycle management — it provides `IDisposable` and `IAsyncLifetime` via protected virtual overrides.

## Performance Tests

- Performance tests live in `Compze.Tests.Performance.Internals`.

## Code Policy Tests

- Code policy tests live in `Compze.Tests.CodePolicies`.


## Integration Tests

- Create a `TestingEndpointHost`, register endpoints, start/stop in lifecycle methods:
  ```csharp
  protected override async Task InitializeAsyncInternal() => await _host.StartAsync();
  protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();
  ```
- Use `IServiceLocator` for resolving services: `ServiceLocator.ExecuteTransactionInIsolatedScope(...)`.
- Use `IThreadGate` for controlling concurrency timing: `gate.Close()`, `gate.Open()`, `gate.AwaitPassedThroughCountEqualTo(n)`.

## Performance Tests

- Use `TimeAsserter.Execute(action, iterations: N, maxTotal: duration)` for timed assertions.
- Use `EnvDivide()` / `EnvMultiply()` on thresholds to adjust for slow/instrumented machines.
- Warm up with `StopwatchCE.TimeExecution()` before measuring.
- Mark performance test projects with `[assembly: PerformanceAttribute]`.
- Test parallelization is disabled in performance test projects.

## Code Policy Tests

- Typically static classes with `[Fact]` methods that scan assemblies for violations.

## Pluggable Component Testing

- **Never** write one test per pluggable component. Use `[PCT]` attribute + `UniversalTestBase` base class — this automatically tests ALL enabled combinations.
- Test methods take zero parameters; access the current combination via the static `TestEnv` class.

