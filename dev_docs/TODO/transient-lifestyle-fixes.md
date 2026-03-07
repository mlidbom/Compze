# Transient / TrackedTransient Lifestyle Fixes

All items fixed. 71/71 DI specification tests pass.

## Bugs (all fixed)

### 1. Tracker registrations bypass `_registeredComponents` — DEFERRED

In `DependencyInjectionContainerBase.Register()`, the `ScopedTransientInstanceTracker` and `SingletonTransientInstanceTracker` are registered via `RegisterInContainer()` directly, skipping `_registeredComponents.AddRange()`. This means:
- `RegisteredComponents()` won't include them
- `ValidateNoDuplicateRegistrations` won't protect against someone accidentally registering the same types
- `AssertLifeStyleCombinationsAreValid` won't validate their dependencies

Adding them to `_registeredComponents` breaks `ContainerCloner` — the clone triggers `_registerTransientInstanceTrackers.RunIfFirstCall()` a second time causing duplicate registration. Needs a more careful solution that either filters them out of `RegisteredComponents()` or marks them as infrastructure-internal registrations that the cloner should skip.

### 2. ~~Race condition in `TransientInstanceTracker` — missing disposal guard~~ FIXED

Added `_isDisposed` flag. `Track()` now calls `Contract.State.NotDisposed(_isDisposed, this)`. Both `Dispose()` and `DisposeAsync()` set the flag before draining.

### 3. ~~Lifestyle validation allows Transient/TrackedTransient → Scoped dependency~~ FIXED

`IsInvalidLifestyleCombination` now returns `true` when consumer is `Transient`/`TrackedTransient` and dependency is `Scoped`.

### 4. ~~`CreateCloneRegistration` drops allow-flags on delegating path~~ FIXED

The `ShouldDelegateToParentWhenCloning` path in `ComponentRegistration<TService>.CreateCloneRegistration` now passes `AllowSingletonDependent` and `AllowScopedDependent` through.

## Test Coverage Gaps (all fixed)

### 5. ~~Duplicate test file~~ FIXED

Deleted `When_resolving_a_transient.cs`. Kept `When_resolving_an_untracked_transient.cs` (clearer name contrasting with tracked).

### 6. ~~Missing async disposal tests for tracked transients~~ FIXED

Added to `When_resolving_a_tracked_transient`:
- `without_a_scope.disposable_instances_are_disposed_when_container_is_disposed_async`
- `without_a_scope.async_disposable_only_instances_are_disposed_when_container_is_disposed_async`
- `within_a_scope.async_disposable_only_instances_are_disposed_when_scope_is_disposed`

### 7. ~~Missing untracked transient dependency tests~~ FIXED

Added to `When_a_transient_depends_on_other_services`:
- `untracked_transient_can_depend_on_a_singleton`
- `untracked_transient_can_depend_on_another_untracked_transient`

### 8. ~~Missing test for transient → scoped dependency behavior~~ FIXED

Added to `LifestyleValidationTests`:
- `Should_throw_when_tracked_transient_depends_on_scoped_service`
- `Should_throw_when_transient_depends_on_scoped_service`
