---
applyTo: "test/**/*.cs,**/*.Tests/**/*.cs"
---

# C# Test Code Conventions for all repositories

## Preferred Style: BDD-Style Specification Tests

- No mocking frameworks — use real implementations with in-memory backing (SQLite in-memory, in-process transports).

The preferred way to write tests is **BDD-style nested specification classes** using `[XF]` (ExclusiveFact).
Many existing tests don't yet follow this style, but all new tests should.

**When adding tests to an existing test class that uses the older flat style:**
- If it's relatively easy, refactor the existing tests in that class to BDD-style nested structure, then add the new tests in that structure.
- If refactoring would be complex or risky, create a new test class using BDD-style structure for the new tests instead.
- Do NOT add new tests to the old flat structure — this only adds to the technical debt

### How it works

Specifications are organized as **nested classes where each level inherits from its parent** to accumulate context. Class names describe the scenario, test method names describe the expected behavior. Each level's **constructor** performs that level's setup which builds upon the setup for the previous level. Tests declared at that level assert the outcome.

`[XF]` (alias for `[ExclusiveFact]`) ensures each test runs **only in the class that declares it** — never in inheriting classes. This prevents the exponential test duplication that plain `[Fact]` causes with inherited tests.

### Complete example

```csharp
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

public class When_a_user_attempts_to_register
{
   readonly RegistrationService _service = new();

   public class with_invalid_email : When_a_user_attempts_to_register
   {
      public class that_is_missing_the_at_sign : with_invalid_email
      {
         readonly RegistrationResult _result;
         public that_is_missing_the_at_sign() => _result = _service.Register("johndoe.com", "Secret123!");

         [XF] public void registration_is_rejected()  => _result.Succeeded.Must().BeFalse();
         [XF] public void error_mentions_email()       => _result.Error.Must().Contain("email");
      }

      public class that_is_empty : with_invalid_email
      {
         readonly RegistrationResult _result;
         public that_is_empty() => _result = _service.Register("", "Secret123!");

         [XF] public void registration_is_rejected()  => _result.Succeeded.Must().BeFalse();
         [XF] public void error_mentions_required()    => _result.Error.Must().Contain("required");
      }
   }

   public class with_valid_data : When_a_user_attempts_to_register
   {
      readonly RegistrationResult _result;
      public with_valid_data() => _result = _service.Register("john@doe.com", "Secret123!");

      [XF] public void registration_succeeds()       => _result.Succeeded.Must().BeTrue();
      [XF] public void a_confirmation_email_is_sent() => _result.ConfirmationEmailSent.Must().BeTrue();
      [XF] public void the_user_id_is_assigned()      => _result.UserId.Must().NotBe(Guid.Empty);
   }
}
```

This produces a readable specification tree in Test Explorer:

```
When_a_user_attempts_to_register
├── with_invalid_email
│   ├── that_is_missing_the_at_sign
│   │   ├── registration_is_rejected
│   │   └── error_mentions_email
│   └── that_is_empty
│       ├── registration_is_rejected
│       └── error_mentions_required
└── with_valid_data
    ├── registration_succeeds
    ├── a_confirmation_email_is_sent
    └── the_user_id_is_assigned
```

### Key rules for BDD-style tests

- **Use `[XF]`**, never `[Fact]`,  — `[Fact]` causes inherited tests to re-run in every descendant.
- **Class names describe context** using lowercase with underscores: `with_invalid_email`, `that_is_empty`, `After_adding_entity`.
- **Method names describe expected behavior** using lowercase with underscores: `registration_is_rejected`, `error_mentions_email`.
- **Each nested class inherits from its parent** to gain access to shared setup.
- **Each level's constructor is the "act"** — it performs that level's specific setup.
- **Tests at each level are only assertions** — single-expression `=>` bodies calling `Must()`.
- **Split large specifications** across partial class files using dot-separated naming: `Specification.Step1.Step2.cs`.

## Framework & Base Class

- **xUnit v3** is the test framework.

## Test Attributes

| Attribute | Purpose |
| --- | --- |
| `[XF]` | **Exclusive Fact — the default for new tests.** Only runs in the declaring class (not inherited), enabling BDD-style nested inheritance. |
| `[PCT]` | Pluggable Component Theory — runs the test for every configured component combination (SqlLayer × DIContainer × Serializer × Transport). |
| `[PCTSerializer]` | Varies only the Serializer component. |
| `[PCTDIContainer]` | Varies only the DIContainer component. |
| `[Performance]` | Marks performance tests. |
| `[LongRunning]` | Marks long-running tests. |

**Never write one test per pluggable component.** Use `[PCT]` + `UniversalTestBase` + `TestEnv` — it automatically tests all enabled combinations with zero-parameter test methods.

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

In BDD-style tests, arrange/act happens in constructors (each nesting level), and test methods are pure assertions. Prefer single-expression test bodies:
```csharp
[XF] public void name_is_root() => _taggregate.Name.Must().Be("root");
```

## Attribute Placement

Short single-expression tests: attribute on the same line as the method:
```csharp
[XF] public void passed_through_is_0() => _fixture.Gate.Passed.Must().Be(0);
```

## Setup & Teardown

- Set up state in the **constructor** — not in a `[SetUp]` or separate method. In BDD-style specs, each nesting level's constructor adds its own context.
- Override these protected virtual methods from `UniversalTestBase` instead of implementing `IDisposable`/`IAsyncLifetime` directly:
  - `DisposeInternal()` — synchronous cleanup.
  - `InitializeAsyncInternal()` — async initialization (e.g., `await Host.StartAsync()`).
  - `DisposeAsyncInternal()` — async cleanup (e.g., `await Host.DisposeAsync()`).

## Async Tests

- Return `Task` or `async Task`.
- **No `.ConfigureAwait(false)` / `.caf()` in test code** — this is suppressed for tests.
- Use `InvokingAsync()` for async exception assertions.

## Test Data

- Inline construction with `new`.
- Numeric ranges: `1.Through(9).Select(...)`.
- `TheoryData<>` for parameterized tests.

## Access Modifiers & Formatting

- Test classes and methods: `public` (xUnit requirement).
- Fields: `readonly` where possible, private by default (no explicit `private`).
- 3-space indentation, file-scoped namespaces, `var` everywhere — same as production code.
