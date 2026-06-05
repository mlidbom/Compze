# Compze.Must: An assertion library with

* **failure messages that do the debugging for you**
* **a trivially easy to extend fluent API**
* **strict-by-default semantics**

## A one-line predicate → a complete diagnosis

```csharp
order.Must().Satisfy(it => it.Status == "Shipped");
```

```
Failing assertion:
order.Must().Satisfy(it => it.Status == "Shipped")

order was an Order with:
JSON:
{
  "Id": "ORD-4417",
  "ItemCount": 3,
  "Status": "Processing"
}
```

No re-running under the debugger to ask "well, what *was* the state of order?"

`Satisfy` reports the expression and the state for *any* predicate

## Asserting on equality 

### A unified diff provides every detail of how state was wrong

```csharp
actual.Must().Be(expected);
```

```
Failing assertion:
actual.Must().Be(expected)
Diff:
--- expected
+++ actual
 {
-  "Balance": 1250.00,
+  "Balance": 1000.00,
   "IsActive": true,
-  "Owner": "Ada"
+  "Owner": "John"
 }

 actual was: [Full details including full json here]
 expected was: [Full details including full json here]
```


## Deep comparison of arbitrary objects graphs, with unified diffs

```csharp
cart.Must().DeepEqual(expectedCart);
```

```
Diff:
--- expected
+++ actual
       "Product": "Bread",
-      "Quantity": 1
-    },
-    {
-      "Price": 3.20,
-      "Product": "Eggs",
-      "Quantity": 12
+      "Quantity": 3
     },
     {
       "Product": "Butter",
       ...
       "Product": "Coffee",
       "Quantity": 2
+    },
+    {
+      "Price": 1.20,
+      "Product": "Sugar",
+      "Quantity": 1
     }

cart was: [Full details including full json here]
expectedCart was: [Full details including full json here]
```

**Bread's quantity changed**, **Eggs went missing**, **Sugar appeared**.

## Pluging your own assertion method into the fluent API is two lines of code

```csharp
public static class MyCustomAssertions
{
   public static IAssertionContext<string> BeAValidEmailAddress(this IAssertionContext<string> @this) => 
      @this.RunAssertion(it => it.Contains('@') && it.Contains('.'));
}
```

Call your assertion: `userInput.Must().BeAValidEmailAddress();`
```
Failing assertion:
userInput.Must().BeAValidEmailAddress()

userInput was a string with the value:
ada.example.com
```

Note the heading: the failure is automatically labelled **`BeAValidEmailAddress`** — your own method's name —
because `RunAssertion` captures it for you. **Every built-in assertion in `Must` is written exactly this way**,
so the assertions you add read just like the ones that ship with the library.

Assertions can also take arguments and render them in the failure message; see
[Custom assertions with arguments](#custom-assertions-with-arguments) for that.

---

## Getting started

```csharp
using Compze.Must;

user.Email.Must().NotBeNullOrEmpty();
result.Must().Be(expected);
users.Must().HaveCount(3);
actual.Must().DeepEqual(expected);
```

Every assertion is an extension method on the value under test. Call `.Must()` on anything to start a chain;
chain further assertions onto the result. A failing assertion throws an `AssertionFailedException` carrying
the rich message; a passing one returns the context so you can keep going:

```csharp
caughtException.Must().NotBeNull().BeExactType<InvalidOperationException>();
```

## `Be` / `NotBe` — equality, checked every which way

`Be` doesn't just call `Equals`. It verifies that two values agree across **every comparison mechanism the
type supports** — `Equals`, `IEquatable<T>`, `==`/`!=`, `IComparable`/`IComparable<T>`, structural equality,
and `GetHashCode` — and reports the first one that disagrees. That catches the classic bug where `Equals` and
`==` (or `GetHashCode`) quietly fall out of sync.

```csharp
var actual = 42;
var expected = 43;
actual.Must().Be(expected);
```

```
Failing assertion:
actual.Must().Be(expected)
Expected 43 but got 42

actual was a System.Int32 with the value: 42
expected was a System.Int32 with the value: 43
the first failing equivalency test was:
   it => Equals(it, expected)
```

For reference equality rather than value equality, use `ReferenceEqual` / `NotReferenceEqual`.

## `DeepEqual` — structural equality with a diff

`DeepEqual` compares two object graphs by serializing both and diffing the result, so you get the unified
diff shown above for arbitrarily deep structures. Two things set it apart:

**It sees private state.** Comparison is by *members*, not by the public surface — so two objects that look
identical from the outside but differ in a private field are correctly reported as different:

```csharp
actual.Must().DeepEqual(expected);
```

```
Diff:
--- expected
+++ actual
 {
   "InternalProperty": "internal_expected",
-  "PrivateField": "private_expected",
+  "PrivateField": "private_actual",
   "PublicProperty": "public_expected"
 }
```

**You choose the visibility scope.** Pick how deep "equal" goes:

| Method | Compares |
|---|---|
| `DeepEqual` / `DeepEqualPrivate` | all members — public, internal, **and private** |
| `DeepEqualInternal` | internal + public members |
| `DeepEqualPublic` | public members only |

And you can tune any comparison with a config callback:

```csharp
// Ignore noisy identity fields:
actual.Must().DeepEqual(expected, config => config.ExcludeTypeMember(it => it.Id));

// Compare structurally, ignoring declared types:
actual.Must().DeepEqual(expected, config => config.IgnoreTypes());
```

## `Satisfy` — the universal assertion

When no built-in assertion fits, `Satisfy` takes any predicate and still gives you the full diagnostic
treatment — the predicate expression and the serialized state of the value (see the very first example). Add a
custom message when you want one:

```csharp
order.Must().Satisfy(it => it.Status == "Shipped");
order.Must().Satisfy(it => it.Total > 0, failureMessage: _ => "Orders must have a positive total");
```

## Asserting that something throws

Wrap the call in `Invoking(...)` (or `InvokingAsync(...)`), then assert on the exception — and keep asserting
on it via `.Which`:

```csharp
Invoking(() => account.Withdraw(1_000_000))
   .Must().Throw<InsufficientFundsException>()
   .Which.Balance.Must().Be(0);

await InvokingAsync(() => repository.SaveAsync(order))
   .Must().ThrowAsync<DbConcurrencyException>();
```

## The rest of the toolbox

| Category | Assertions |
|---|---|
| Equality | `Be`, `NotBe`, `DeepEqual` (+ `Public`/`Internal` scopes), `ReferenceEqual`, `NotReferenceEqual` |
| Nullability | `NotBeNull` (narrows to the non-null type), `BeNull` |
| Boolean | `BeTrue`, `BeFalse` |
| Strings | `Contain`, `NotContain`, `StartWith`, `EndWith`, `BeNullOrEmpty`, `NotBeNullOrEmpty`, `NotBeNullOrWhiteSpace` |
| Collections | `HaveCount`, `BeEmpty`, `NotBeEmpty`, `Contain`, `SequenceEqual` |
| Comparison | `BeGreaterThan`, `BeGreaterThanOrEqualTo`, `BeLessThan`, `BeLessThanOrEqualTo`, `BePositive`, `BeNegative` |
| Type | `BeExactType<T>`, `BeAssignableTo<T>` (both narrow the context to `T`) |
| Value sets | `BeOneOf`, `BeValidEnumValue` |
| Throwing | `Invoking(...).Must().Throw<T>()`, `InvokingAsync(...).Must().ThrowAsync<T>()` |
| General | `Satisfy` |

## Custom assertions with arguments

An assertion can take arguments and surface them in the failure message with their resolved values: pass them
as `expressionValues`, capturing each argument's source text with `[CallerArgumentExpression]`.

```csharp
public static IAssertionContext<int> BeInRange(this IAssertionContext<int> @this, int min, int max,
                                               [CallerArgumentExpression(nameof(min))] string minExpression = null!,
                                               [CallerArgumentExpression(nameof(max))] string maxExpression = null!) =>
   @this.RunAssertion(it => it >= min && it <= max,
                      expressionValues: [new(minExpression, min), new(maxExpression, max)]);
```

```csharp
age.Must().BeInRange(minimumAge, maximumAge);
```

```
Failing assertion:
age.Must().BeInRange(minimumAge, maximumAge)

age was a System.Int32 with the value: 17
minimumAge was a System.Int32 with the value: 18
maximumAge was a System.Int32 with the value: 65
```

`RunAssertion` is the primitive every built-in assertion is built on, and it's why a custom assertion reports
its *own* name rather than `Satisfy`: `Satisfy` always labels failures `Satisfy`, whereas `RunAssertion`
surfaces the calling method's name and the `expressionValues` you pass — automatically.

---

## Tips & considerations

### Property or object? A judgment call

Every assertion is a choice of *subject*, and the subject decides how much you see when it fails:

- **Assert on a property** — `order.Status.Must().Be("Shipped")` — less output to parse, pointed right at the
  value that's wrong; but the state of the owning object is missing in action.
- **Assert on the owning object** — `order.Must().Satisfy(it => it.Status == "Shipped")` — the full context of
  the object's state, right there in the failure; but you have to dig out what the state was from a potentially
  long bit of JSON.

Two things worth knowing when you make that call:

- It's about the *subject*, not about `Satisfy`. A **custom assertion built on `RunAssertion` keeps the same
  full-object context** — so reaching for a reusable, named assertion never costs you the state.
- For comparing whole objects, prefer `Be` / `DeepEqual`: their unified diff beats a `Satisfy` equality
  predicate. (And the object is only serialized on failure — passing tests pay nothing.)

---

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Full testing infrastructure |
| [Compze.xUnitMatrix](https://www.nuget.org/packages/Compze.xUnitMatrix) | xUnit utilities |

## License

Apache-2.0
