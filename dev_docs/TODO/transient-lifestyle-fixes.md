# Transient / TrackedTransient Lifestyle Fixes

## Bugs

### 1. Tracker registrations bypass `_registeredComponents`

In `DependencyInjectionContainerBase.Register()`, the `ScopedTransientInstanceTracker` and `SingletonTransientInstanceTracker` are registered via `RegisterInContainer()` directly, skipping `_registeredComponents.AddRange()`. This means:
- `RegisteredComponents()` won't include them
- `ValidateNoDuplicateRegistrations` won't protect against someone accidentally registering the same types
- `AssertLifeStyleCombinationsAreValid` won't validate their dependencies

**Fix:** Route them through the normal `Register()` path, or explicitly add them to `_registeredComponents`.

### 2. Race condition in `TransientInstanceTracker` — missing disposal guard

`Track()` can be called after `Dispose()` starts draining the bag, leaking instances. Add a `_disposed` flag and use `Contract.State.NotDisposed` from Compze.Contracts in `Track()` to reject late arrivals.

### 3. Lifestyle validation allows Transient/TrackedTransient → Scoped dependency

`IsInvalidLifestyleCombination` returns `false` (always valid) when the consumer is `Transient` or `TrackedTransient`, regardless of the dependency's lifestyle. A transient depending on a scoped service passes validation but blows up at runtime if resolved outside a scope.

**Fix:** When consumer is `Transient`/`TrackedTransient` and dependency is `Scoped`, flag it as invalid (or at least require an explicit opt-in).

### 4. `CreateCloneRegistration` drops allow-flags on delegating path

In `ComponentRegistration<TService>.CreateCloneRegistration`, the `ShouldDelegateToParentWhenCloning` path creates a new singleton-instance registration but does not pass `AllowSingletonDependent` or `AllowScopedDependent`. If the original registration had those flags, the clone silently loses them.

**Fix:** Pass the flags through on both paths.

## Test Coverage Gaps

### 5. Duplicate test file

`When_resolving_a_transient.cs` and `When_resolving_an_untracked_transient.cs` test identical code paths with identical assertions. Delete one.

### 6. Missing async disposal tests for tracked transients

- No `DisposeAsync` test for tracked transients **within a scope** (only sync `IDisposable` tested in scope).
- No `DisposeAsync` test at the **container level** — all tracked transient tests use `container.Dispose()`, never `await container.DisposeAsync()`. The `TransientInstanceTracker.DisposeAsync` codepath is untested.

### 7. Missing untracked transient dependency tests

`When_a_transient_depends_on_other_services` only uses `TrackedTransient`. No equivalent for untracked `Transient` depending on another `Transient`.

### 8. Missing test for transient → scoped dependency behavior

Once bug #3 is fixed, add tests documenting the expected validation behavior when a transient depends on a scoped service.
