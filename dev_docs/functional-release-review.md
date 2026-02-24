# Release Readiness Review: Compze.Functional

**Target version:** 0.8.0-beta.1  
**Review date:** 2026-02-24  
**Verdict:** Very close — a few items to address first.

---

## What's Solid

### Code quality is high

The API surface is small, focused, and well-designed:

- **`Pipe`** — `_()`, `_tap()`, `_mutate()`, `_mutateAsync()`, `_then()` (3 overloads), `_assert()` (2 overloads). Clean, correct, good XML docs.
- **`unit`** — Full value-type semantics: equality, hashing, `ToString()`, `From()`, `Func()` (0–2 params), `AsyncFunc()` (0–2 params). `ConfigureAwait(false)` used consistently.
- **`ActionFuncConverter`** — Symmetric Action↔Func bridging for sync (0–2 params) and async (0–2 params).
- **`ObjectCE`** — Two utility extensions (`_repeat`, `ToStringNotNull`).

### Tests are thorough

47 specifications pass, covering every public method with behavioral tests including edge cases (negative repeat count, exception messages, reference identity, async behavior). The BDD-style nested class structure reads well as living documentation.

### README is excellent

Clear motivation, code examples, naming rationale, and "Related packages" section. It will pack into the NuGet package via `Directory.Build.props`.

### Packaging metadata

Inherited properly from `Directory.Build.props` (Authors, License, RepositoryUrl, README, symbols).

---

## Issues to Fix Before 0.8.0-beta.1


### 3. Missing `_assert` overload

The simplest overload — `_assert(Predicate<T>)` with no custom message/exception — is absent. Users get no zero-friction way to write `value._assert(v => v > 0)`. They must always provide either a message factory or exception factory. Consider whether to add a default-message overload for convenience, or if the current design is intentional (forcing users to write meaningful messages).

### 4. `PackageTags` not relevant for this package

`Directory.Build.props` sets `PackageTags` to `compze;messaging;event-sourcing;cqrs` — not relevant for a standalone functional programming package. If Compze.Functional is intended to be general-purpose (its README positions it that way), it should have its own tags like `functional;pipe;unit;extensions`.

## Summary

The **core API design and implementation are beta-ready**. The `Pipe`, `unit`, and `ActionFuncConverter` types are well-crafted, well-tested, and well-documented. The issues above are mostly cleanup (empty file, stale ncrunch config, wrong tags) and one API question (`_assert` convenience + `tessageFactory` naming). Fix those and it's ready for 0.8.0-beta.1.
