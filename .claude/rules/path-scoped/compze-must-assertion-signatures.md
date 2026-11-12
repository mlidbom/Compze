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

- **The receiver takes the nullable form:** `IAssertionContext<string?>`, or `IAssertionContext<IEnumerable<TElement>?>`
  for a collection assertion (below). A reference constraint is `class?`, never `class`. A receiver of an unconstrained
  generic `IAssertionContext<TValue>` already accepts both.
- **A collection assertion takes `IAssertionContext<IEnumerable<TElement>?>` and returns
  `IAssertionContext<IEnumerable<TElement>>`,** never a `TCollection` type parameter constrained to
  `IEnumerable<TElement>`. C# infers type arguments without looking at constraints, so with a `TCollection` the element
  type comes from the expected values alone: `SequenceEqual(["a"])` on a `List<string?>` warns CS8631, and `[]`, values
  of a narrower type than the subject's elements, and values of several types don't compile at all. `IAssertionContext`
  is covariant, so the receiver form takes the element type from the subject too. Its price is the subject's own
  collection type, which no assertion chained after a collection assertion was found to need, and struct collections
  such as `ImmutableArray<T>`, which reach the assertions through `.AsEnumerable()`.
- **A null subject is a value the assertion reports:** the subject is what the code under test produced, so a null one
  is that code's failure, not the test's. It fails with an `AssertionFailedException`, never with a
  `NullReferenceException` from inside the predicate, unless the assertion asks for null, as `BeNull` and
  `BeNullOrEmpty` do.
- **An assertion that needs a value is built on `RunAssertionThatANullValueFails`.** It fails a null value itself, with
  a message saying the assertion had no value to check, hands the predicate only a value, and returns the context
  narrowed to the non-nullable type. Never write the null check and the narrowing `!` by hand: nothing ties the two
  together, so a dropped check still compiles. Where null can pass (`Be`, `ReferenceEqual`, `BeNull`), the assertion is
  built on `RunAssertion` and its return keeps the receiver's nullability. The assertions about null itself,
  `NotBeNull`, `NotBeNullOrEmpty` and `NotBeNullOrWhiteSpace`, narrow on their own check: their predicate is the null
  check, and "had no value to check" would misdescribe their failure.
- **An expected value takes null only where null is a meaningful expectation:** `Be`'s and `ReferenceEqual`'s do, and
  so does a collection `Contain`'s item. `StartWith`'s, `SequenceEqual`'s and the comparisons' don't: there the
  parameter is non-nullable, so the compiler warns about a value that may be null, and a null that arrives anyway is a
  bug in the test. Assert it first with `Argument.NotNull(expected, expectedExpression)`, which throws an
  `ArgumentNullException` naming the test's expression. Left unasserted, a null can even pass: .NET orders null before
  every string, so `BeGreaterThan(null)` succeeded.
- **No type-specific overload of a generic assertion.** It captures every call for that type, nullable ones included,
  as the string overload of `Be` did. Specialize inside the generic assertion, at run time: `Be` compares two strings
  ordinally and shows a diff of their text, and `Contain` asks a read-only set itself, so its own comparer decides.
- **A nullable value type is reached through `NotBeNull()`,** which narrows `T?` to `T`. `int?` is a different type from
  `int`, so an assertion over a value type gets no second overload for its nullable form.
- **A warning that tells the truth stays:** `Satisfy(it => it.Length == 1)` on a `string?` warns because the predicate
  dereferences a value that may be null; the consumer writes `NotBeNull().Satisfy(…)`.

## Verifying

A new or changed assertion gets its cases in each of the three specifications in
`test/Compze.Must.Specifications/Nullability/`:

- `When_a_consumer_compiles_an_assertion` compiles consumer code against Compze.Must with Roslyn (`ConsumerCode`) and
  pins which call shapes compile without warnings: on a subject declared nullable, its narrowed `Actual` where it
  narrows, and on a non-nullable subject. Declare the subject as a parameter of the consumer's method, never as a
  local: a local's nullability follows what was assigned to it, so `string? subject = "x";` is a non-nullable subject
  to the compiler.
- `When_asserting_on_a_subject_declared_nullable`: what it does with a null subject, and with one that holds a value.
- `When_an_assertion_is_given_a_null_expected_value`: an `ArgumentNullException` for each expected value it asserts.
