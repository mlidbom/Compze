---
applyTo: "test/**/*.cs"
---

# C# Test Code Conventions

## Framework & Base Class

- **xUnit v3** is the test framework.
- **Inherit from `UniversalTestBase`** — it provides `IDisposable` and `IAsyncLifetime` via protected virtual overrides.
- No mocking frameworks — use real implementations via our PCT support.

## Test Attributes

| Attribute | Purpose |
| --- | --- |
| `[XF]` | Exclusive Fact. The default attribute to usef for non PCT tests. Only runs in the declaring class (not inherited), enabling nesting inherihiting tests for BDD style testing. |
| `[PCT]` | Pluggable Component Theory — runs the test for every configured component combination (SqlLayer × DIContainer × Serializer × Transport). |
| `[PCTSerializer]` | Varies only the Serializer component. |
| `[PCTDIContainer]` | Varies only the DIContainer component. |
| `[Performance]` | Marks performance tests. |
| `[LongRunning]` | Marks long-running tests. |

**Never write one test per pluggable component.** Use `[PCT]` + `UniversalTestBase` + `TestEnv` — it automatically tests all enabled combinations with zero-parameter test methods.

## Test Method Naming

- **Underscores for readability**: `My_test_method_does_something()`.
- Descriptive sentence-style names are preferred: `If_tommand_handler_throws_disposing_host_throws_AggregateException()`.

## Assertions

Use the custom **`Must`** assertion library — not xUnit `Assert` or FluentAssertions:

```csharp
value.Must().Be(expected);
value.Must().NotBeNull();
value.Must().BeTrue();
collection.Must().HaveCount(5);
```

### Exception assertions
Import `using static Compze.Utilities.Testing.Must.MustActions;` then:
```csharp
Invoking(() => action()).Must().Throw<SomeException>();
await InvokingAsync(async () => await asyncAction()).Must().ThrowAsync<SomeException>();
```
Chain into caught exceptions with `.Which`:
```csharp
Invoking(() => ...).Must().Throw<Exception>().Which.Message.Must().Contain("text");
```

## Arrange/Act/Assert

**Do NOT add `// Arrange`, `// Act`, `// Assert` comments.** The pattern is implicit.

Prefer single-expression test bodies when possible:
```csharp
[XF] public void Name_is_root() => _taggregate.Name.Must().Be("root");
```

## Attribute Placement

Short single-expression tests: attribute on the same line as the method:
```csharp
[XF] public void PassedThrough_is_0() => _fixture.Gate.Passed.Must().Be(0);
```

## Setup & Teardown

- Set up state in the **constructor** — not in a `[SetUp]` or separate method.
- Override these protected virtual methods from `UniversalTestBase` instead of implementing `IDisposable`/`IAsyncLifetime` directly:
  - `DisposeInternal()` — synchronous cleanup.
  - `InitializeAsyncInternal()` — async initialization (e.g., `await Host.StartAsync()`).
  - `DisposeAsyncInternal()` — async cleanup (e.g., `await Host.DisposeAsync()`).

## BDD-Style Nested Classes

For complex specifications, use nested partial classes with inheritance to accumulate state:
```csharp
public partial class After_constructing_root
{
   public partial class After_adding_entity : After_constructing_root
   {
      // inherits accumulated state, adds more setup in constructor
   }
}
```
Split across files using dot-separated naming: `Specification.Step1.Step2.cs`.

## Async Tests

- Return `Task` or `async Task`.
- **No `.ConfigureAwait(false)` / `.caf()` in test code** — this is suppressed for tests.
- Use `InvokingAsync()` for async exception assertions.

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
- Performance tests live in `Compze.Tests.Performance.Internals` with `[assembly: PerformanceAttribute]`.
- Test parallelization is disabled in performance test projects.

## Code Policy Tests

- Live in `Compze.Tests.CodePolicies`.
- Typically static classes with `[Fact]` methods that scan assemblies for violations.

## Test Data

- Inline construction with `new`.
- Numeric ranges: `1.Through(9).Select(...)`.
- `TheoryData<>` for parameterized tests.

## Access Modifiers & Formatting

- Test classes and methods: `public` (xUnit requirement).
- Fields: `readonly` where possible, private by default (no explicit `private`).
- 3-space indentation, file-scoped namespaces, `var` everywhere — same as production code.
