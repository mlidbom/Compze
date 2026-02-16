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

### The goal: readable specifications

The overriding goal is that **the full path from namespace through classes to test method reads as a clear specification sentence**. There is no rigid structural template to follow — no required AAA shape, no mandatory "Given/When/Then" vocabulary. Any sentence structure works as long as the result reads like a specification and the code is simple, clean, and actually does what the spec line says.

Example spec lines:
- `Specifications.Contracts.AssertionMethods.NotNull.Throws_when_called_with.null_string`
- `Specifications.UserAccounts.Registration.When_a_user_attempts_to_register.with_valid_data.registration_succeeds`
- `Specifications.Sql.TeventStore.ReadOrder.Parsing.after_calling_ToString.and_converting_back_to_ReadOrder.the_value_is_identical_to_the_original_value`

### How it works

Specifications are organized using **namespaces/folders for categorization** and/or **nested classes**. 
Each nested class inherits from its parent, and, often, each level's **constructor** performs that level's setup, building upon the parent's setup. 
Note that there is no requirement that each level has a constructor though, a `AssertionMethods.NotNull.Throws_when_called_with.null_string` where NotNull is the root of the nested classes is perfectly fine for specifying the behavior of a method and will probably have zero setup. 
Tests declared at a level assert the outcome of the accumulated context.

`[XF]` (alias for `[ExclusiveFact]`) ensures each test runs **only in the class that declares it** — never in inheriting classes. This prevents the exponential test duplication that plain `[Fact]` causes with inherited tests.

### Complete example

```csharp
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace OurApplication.Specifications.UserAccounts.Registration;

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
OurApplication
└── Specifications
    └── UserAccounts
        └── Registration
            └── When_a_user_attempts_to_register
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

### Folders/namespaces vs. nested classes

Use the right tool for each level in the spec path:

- **Folders/namespaces** = categorization ("what area are we testing?"). No executable context — just organization.
- **Nested classes** = accumulated context ("given this setup, what happens next?"). Each level's constructor builds on the parent's state.

**If a nesting level has no constructor body** (or its constructor doesn't build on the parent's state), it's just categorization — use a folder/namespace instead. This keeps files small, reduces indentation depth, and makes navigation easier.

Example: `Specifications.Contracts.AssertionMethods.NotNull` is all namespace/folder (categorization), then `ThrowsWhenCalledWith` is a nested class (shared setup context), and `null_string` is the test method (specific case).

### File & namespace organization

- **One root-level test class per file.** The file name must match the root class name: `When_a_user_attempts_to_register.cs` contains `public class When_a_user_attempts_to_register`.
- **Use namespace hierarchy to build readable spec lines.** The full path from namespace through nested classes to test method should read as a coherent specification sentence. Use subdirectories matching namespace segments.
  - Good: `Specifications.UserAccounts.Registration.When_a_user_attempts_to_register.with_valid_data.registration_succeeds`
  - Bad: `Specifications.When_a_user_attempts_to_register.with_valid_data.registration_succeeds` (too flat — what domain area?)
- **Group related spec files** under a shared namespace/directory when they cover a cohesive feature area.

### Naming accuracy

Names are the specification — they must precisely match reality:

- **Class names must describe the actual setup**, not an idealized or generalized version. If the constructor registers with an email missing the `@` sign, say `that_is_missing_the_at_sign`, not `with_bad_format`. If it sets up both an invalid email and a weak password, don't call it `verifying_validation` — call it `with_invalid_email_and_weak_password`.
- **Test method names must describe what is actually asserted.** If the assertion checks that both email and password errors are returned, don't name it `email_error_is_returned` — name it `both_validation_errors_are_returned`.

## Framework & Base Class

- **xUnit v3** is the test framework.

## Test Attributes

| Attribute | Purpose |
| --- | --- |
| `[XF]` | **Exclusive Fact — the default for new tests.** Only runs in the declaring class (not inherited), enabling BDD-style nested inheritance. |
| `[Fact]` / `[Theory]` | Standard xUnit — only for simple utility-level tests that don't use nesting. |

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

**Do NOT add `// Arrange`, `// Act`, `// Assert` comments.** There is no required AAA structure — the spec sentence is the structure. In BDD-style tests, setup happens in constructors (each nesting level) and test methods are pure assertions. Prefer single-expression test bodies:
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
