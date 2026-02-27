# Compze.Contracts

Fluent, chainable runtime assertions for preconditions, invariants, and state checks — with `CallerArgumentExpression` support for clear failure messages.

### Assertion types each supporting every assert method

| Entry point | Throws on failure | Use for |
|---|---|---|
| `Contract.Argument` | `ArgumentAssertionFailedException` / `ArgumentNullException` | Method parameter validation |
| `Contract.State` | `StateAssertionFailedException` | Enforcing state requirements for the requested operation|
| `Contract.Invariant` | `InvariantViolatedException` | Class invariant enforcement |

All entry points return a `ContractAsserter` that supports fluent chaining.

### Assertion methods

| Method | Description |
|---|---|
| `.Assert(condition, ...)` | Boolean checks |
| `.Assert(condition, messageFactory)` | Boolean check with lazy message creation on failure |
| `.NotNull(value)` | Null check (also `NotNull2`, `NotNull3`, `NotNull4` for multiple values) |
| `.NotDefault(value)` | Default-value check for structs (also 2/3/4-value overloads) |
| `.NotNullOrEmpty(string)` | Rejects null or empty strings |
| `.NotNullEmptyOrWhitespace(string)` | Rejects null, empty, or whitespace-only strings |
| `.NotDisposed(isDisposed, instance)` | Throws `ObjectDisposedException` |

**All the assertion methods that do not take an explicit separate argument for the message use `CallerArgumentExpression` to ensure that you can know exactly what assertion failed**

### Pipeline assertions

For inline assertions, avoiding the need for duplicate lines and breaking out of the fluent style:

```csharp
var name = GetName()._assert(it => it.Length > 0);          // predicate with auto-generated message
var id   = GetId()._assert(it => it != Guid.Empty, it => $"Invalid id: {it}");      // predicate with custom message factory
var conn = GetConnection()._assert().NotNull(); 
```

Pipeline overloads:
- `value._assert()` — returns `AssertionTarget<T>` for `.NotNull()` / `.NotDefault()` chains
- `value._assert(predicate)` — throws `AssertionFailedException("value._assert(predicate)")` on failure
- `value._assert(predicate, messageFactory)` — throws `AssertionFailedException` with custom message
- `value._assert(predicate, exceptionFactory)` — throws custom exception

**All the assertion methods that do not take an explicit separate argument for the message use `CallerArgumentExpression` to ensure that you can know exactly what assertion failed**

### Quick start

#### Fluent with static use. No wasted lines. The contract reads like part of the method declaration.
```csharp
void Transfer(Account from, Account to, decimal amount) => 
    Contract.Argument.NotNull2(from, to).Assert(amount > 0).State.NotDisposed(_disposed, this)._then(() => {
        //method implementation here
    });
```
**Note, the _then method is from Compze.Fluent. You may want to check it out.**

#### Classic style. Pretty verbose in our opinion
```csharp
void Transfer(Account from, Account to, decimal amount)
{
    Contract.Argument.NotNull2(from, to);
    Contract.Argument.Assert(amount > 0);
    Contract.State.NotDisposed(_disposed, this);
    //method implementation here
}
```

#### Mixing it up with Compze.Fluent
```csharp
public OperationResult SomeBusinessMethod(Guid userId) =>
    userId
    ._assert().NotDefault()
    ._(LoadFromDatabase)
    ._tap(it => { /*log*/ })
    ._assert(MayExecuteThisOperation)
    ._(ActualOperationLogic)
    ._tap(it => { /*log*/ })
    ._assert(ResultIsWhatWeExpected);
```

### Custom assertion extensions

Both `ContractAsserter` and `AssertionTarget<T>` are designed to be extended. All built-in assertions are themselves extension methods.

#### ContractAsserter extension
```csharp
public static class MyContractExtensions
{
   extension(ContractAsserter @this)
   {
      public ContractAsserter IsValidEmail(string? value,
                                           [CallerArgumentExpression(nameof(value))] string expression = "")
      {
         if(!value.IsValidEmail()) @this.ThrowFailed(expression);
         return @this;
      }
   }
}

//throws: ArgumentAssertionFailedException("Argument.IsValidEmail(userEmail)") if invalid.
Contract.Argument.IsValidEmail(userEmail); 
```

#### AssertionTarget extension (pipeline)
```csharp
public static class MyPipelineExtensions
{
   public static string IsValidEmail(this AssertionTarget<string?> target)
   {
      if(!target.Value.IsValidEmail()) target.ThrowAssertionFailed();
      return target.Value!;
   }
}

// throws:  AssertionFailedException("GetUserEmail()._assert().IsValidEmail()") if invalid.
var email = GetUserEmail()._assert().IsValidEmail();
```


## License

Apache-2.0
