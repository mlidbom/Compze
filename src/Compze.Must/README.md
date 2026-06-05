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

The predicate you wrote is **echoed back verbatim**, and the **entire object is serialized right there**.
No re-running under the debugger to ask "well, what *was* the status?" — and because `Satisfy` reports the
expression and the state for *any* predicate, you rarely need to write a purpose-built assertion at all.

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
```
```The full json of actual here```

```The full json of expected here here```

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
```

**Bread's quantity changed**, **Eggs went missing**, **Sugar appeared**.

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

## Extending: write your own assertion in two lines

Custom assertions are just extension methods on `IAssertionContext<T>` that call **`RunAssertion`** — the same
primitive every built-in is built on. Because `RunAssertion` captures the calling method's name and arguments
for you, your assertion gets the full diagnostic treatment automatically, with **zero message-formatting code**:

```csharp
public static class MustAssertions
{
   public static IAssertionContext<string> BeAValidEmailAddress(this IAssertionContext<string> @this) =>
      @this.RunAssertion(it => it.Contains('@') && it.Contains('.'));
}
```

```csharp
userInput.Must().BeAValidEmailAddress();
```

```
Failing assertion:
"ada.example.com".Must().BeAValidEmailAddress()

"ada.example.com" was a string with the value:
ada.example.com
```

Note the heading: the failure is labelled **`BeAValidEmailAddress`** — your method's own name, not "Satisfy".
That's the difference between `RunAssertion` and `Satisfy`: `Satisfy` always reports itself as `Satisfy`,
whereas `RunAssertion` surfaces the name of *your* assertion. (This is why the library uses `RunAssertion`
internally rather than `Satisfy`, and it's why your custom assertions read just like the built-in ones.)

Surface the arguments too, and they're rendered in the failure with their resolved values:

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

---

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Full testing infrastructure |
| [Compze.xUnitMatrix](https://www.nuget.org/packages/Compze.xUnitMatrix) | xUnit utilities |

## License

Apache-2.0
