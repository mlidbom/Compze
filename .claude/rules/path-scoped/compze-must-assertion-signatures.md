---
paths:
  - "src/Compze.Must/**/*.cs"
  - "test/Compze.Must.Specifications/**/*.cs"
---

# Compze.Must assertion signatures: accept any nullability, narrow only on proof

**Every assertion accepts its subject whatever nullability the subject is declared with, and returns a non-nullable
context exactly when passing it proves the value is not null.** A consumer then never writes `subject!.Must()`, and no
call shape warns.

**Why it has to be one rule, not a choice made per signature:** C# picks an overload without looking at nullable
annotations and checks them only afterwards, and two overloads can't differ only in `string` versus `string?`. So a
signature that takes the non-nullable form warns for every nullable caller, and a fix that changes one signature to suit
one call site moves the warning to another.

## How

- **The receiver takes the nullable form:** `IAssertionContext<string?>`, `IAssertionContext<IEnumerable<TItem>?>`, or
  `IAssertionContext<TCollection?>` with the constraint on `TCollection`. A reference constraint is `class?`, never
  `class`. A receiver of an unconstrained generic `IAssertionContext<TValue>` already accepts both.
- **A null value is a value the assertion reports:** it fails with an `AssertionFailedException`, never with a
  `NullReferenceException` from inside the predicate (`it => it is not null && …`), unless the assertion asks for null,
  as `BeNull` and `BeNullOrEmpty` do.
- **The return is narrowed only when passing proves the value is not null:** an `IAssertionContext<string>` from an
  `IAssertionContext<string?>` receiver, by ending the `RunAssertion` call with `!`, as `NotBeNullOrEmpty` does. Where
  null can pass (`Be`, `ReferenceEqual`, `BeNull`), the return keeps the receiver's nullability.
- **An expected value takes null only where null is a meaningful expectation:** `Be`'s and `ReferenceEqual`'s do;
  `Contain`'s and `StartWith`'s don't, so passing one that may be null warns, truthfully.
- **No type-specific overload of a generic assertion.** It captures every call for that type, nullable ones included,
  as the string overload of `Be` did. Specialize inside the generic assertion, at run time: `Be` compares two strings
  ordinally and shows a diff of their text.
- **A nullable value type is reached through `NotBeNull()`,** which narrows `T?` to `T`. `int?` is a different type from
  `int`, so an assertion over a value type gets no second overload for its nullable form.
- **A warning that tells the truth stays:** `Satisfy(it => it.Length == 1)` on a `string?` warns because the predicate
  dereferences a value that may be null; the consumer writes `NotBeNull().Satisfy(…)`.

## Verifying

`test/Compze.Must.Specifications/Nullability/When_a_consumer_compiles_an_assertion.cs` compiles consumer code against
Compze.Must with Roslyn (`ConsumerCode`) and pins which call shapes compile without warnings. A new or changed
assertion gets its cases there: on a subject declared nullable, its narrowed `Actual` where it narrows, and on a
non-nullable subject. Declare the subject as a parameter of the consumer's method, never as a local: a local's
nullability follows what was assigned to it, so `string? subject = "x";` is a non-nullable subject to the compiler.
