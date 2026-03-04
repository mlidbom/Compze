# Compze.Utilities.Testing.XUnit

**BDD-style specification testing for xUnit v3** — write nested, inheritable test classes that read like executable specifications.

## Tests that become an easy to read specification

Specifications are organized as nested classes where each level adds context. Namspaces + class names describe the scenario, test method names describe the expected results:

### In the Test Explorer or in reports this produces something like this:

```
OurApplication
└── Specifications
    └── UserAccounts
        └── Registration
            └── When_a_user_attempts_to_register
                ├── with_invalid_email
                │   ├── that_is_missing_the_at_sign
                │   │   ├── registration_is_rejected
                │   │   |── error_mentions_email
                |   |   └── error_mentions_at_character
                │   └── that_is_empty
                │       ├── registration_is_rejected
                │       └── error_mentions_required
                ├── with_invalid_password
                │   ├── that_is_too_short
                │   │   ├── registration_is_rejected
                │   │   └── error_mentions_password_length
                │   └── that_has_no_digit
                │       ├── registration_is_rejected
                │       └── error_mentions_digit
                └── with_all_valid_data
                    ├── registration_succeeds
                    ├── a_confirmation_email_is_sent
                    └── the_user_id_is_assigned
```


### Tests in this style look like this

```csharp
using Compze.Must;
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
         [XF] public void error_mentions_at_character()       => _result.Error.Must().Contain("@");
      }

      public class that_is_empty : with_invalid_email
      {
         readonly RegistrationResult _result;
         public that_is_empty() => _result = _service.Register("", "Secret123!");

         [XF] public void registration_is_rejected()  => _result.Succeeded.Must().BeFalse();
         [XF] public void error_mentions_required()    => _result.Error.Must().Contain("required");
      }
   }

   public class with_invalid_password : When_a_user_attempts_to_register
   {
      public class that_is_too_short : with_invalid_password
      {
         readonly RegistrationResult _result;
         public that_is_too_short() => _result = _service.Register("john@doe.com", "Ab1!");

         [XF] public void registration_is_rejected()     => _result.Succeeded.Must().BeFalse();
         [XF] public void error_mentions_password_length() => _result.Error.Must().Contain("at least 8");
      }

      public class that_has_no_digit : with_invalid_password
      {
         readonly RegistrationResult _result;
         public that_has_no_digit() => _result = _service.Register("john@doe.com", "SecretPassword!");

         [XF] public void registration_is_rejected()  => _result.Succeeded.Must().BeFalse();
         [XF] public void error_mentions_digit()       => _result.Error.Must().Contain("digit");
      }
   }

   public class with_all_valid_data : When_a_user_attempts_to_register
   {
      readonly RegistrationResult _result;
      public with_all_valid_data() => _result = _service.Register("john@doe.com", "Secret123!");

      [XF] public void registration_succeeds()       => _result.Succeeded.Must().BeTrue();
      [XF] public void a_confirmation_email_is_sent() => _result.ConfirmationEmailSent.Must().BeTrue();
      [XF] public void the_user_id_is_assigned()      => _result.UserId.Must().NotBe(Guid.Empty);
   }
}
```
note: We use our Must fluent assertion library in this code. You may want to check it out if you like our approach to testing...

Each `[XF]` test runs **exactly once** — in the class that declares it — even though child classes inherit parent members.

## Why do you need a separate library to do this?

xUnit runs every `[Fact]` it finds on a class, *including inherited ones*. So if you nest-inherit test classes in this way it causes an exponential *explosion* of redundant test executions and an unreadable specification. This makes BDD-style testing impractical.


### How context flows through inheritance

The key technique: each nested class **inherits from its parent**, gaining access to the shared setup (here, `_service`). Each level's **constructor** adds its own specific context — the "act" step in each scenario. Tests declared at that level assert the outcome. Because `[XF]` skips inherited tests, only the assertions *declared* in each class execute there.

## How it works

`[ExclusiveFact]` / `[XF]` is an xUnit v3 `[Fact]` with a custom discoverer. During discovery it compares the declaring type of the test method against the current test class. If they differ (i.e. the test was inherited), the test case is simply not emitted. No reflection hacks, no runtime skipping — just clean xUnit extensibility.

## Key benefits

- **Reads like a specification** — class names are the context ("Given…", "When…", "And…"), method names are the assertions
- **No duplicated runs** — inherited tests are silently excluded at discovery time
- **Shared setup via constructors and inheritance** — each nested class adds context on top of the parent, just like BDD `context` blocks
- **Works with standard xUnit tooling** — Test Explorer, `dotnet test`, CI — everything sees a clean hierarchy
- **Zero ceremony** — swap `[Fact]` for `[XF]`, nest your classes, done
- **xUnit v3 native** — built on the v3 extensibility APIs (`IXunitTestCaseDiscoverer`)

## Installation

```shell
dotnet add package Compze.Utilities.Testing.XUnit
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Must](https://www.nuget.org/packages/Compze.Must) | Fluent assertions (`Must().Be()`, `Must().Throw<>()`, etc.) |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Full integration testing infrastructure |
| [Compze.Utilities.Testing.DbPool](https://www.nuget.org/packages/Compze.Utilities.Testing.DbPool) | Database pool management for tests |

## License

Apache-2.0
