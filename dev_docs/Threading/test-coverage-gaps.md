# Test Coverage Gaps: Compze.Threading + Compze.InterprocessObject

Review of the full public API surface against existing test files. Matrix coverage (tracked in `test-matrix-coverage.md`) is complete — these are gaps outside the matrix system.

---

## Gap 1: `IMutex` — Local scope cross-instance synchronization untested

`Two_IMutex_instances_with_the_same_Global_name.synchronize_with_each_other` exists for Global. No equivalent test for Local scope — verifying that two `IMutex.Local()` instances with the same name synchronize within the same session.

---

## Gap 2: `LockTimeout` / `WaitTimeout` — no dedicated value type specs

These strongly-typed duration wrappers have factory methods, operators (`==`, `!=`), implicit conversions to `TimeSpan`, `ToString()`, and validation logic (e.g., `LockTimeout` rejects `InfiniteTimeSpan`). None of this is systematically tested. They're exercised indirectly through lock specs, but edge cases aren't covered:
- `LockTimeout` constructor throws on `Timeout.InfiniteTimeSpan`
- Equality operators
- Implicit `TimeSpan` conversions
- `WaitTimeout.Infinite` and `WaitTimeout.IsInfinite`

---

## Priority Assessment

| Gap | Impact | Effort | Priority |
|-----|--------|--------|----------|
| 8. Local mutex cross-instance | Low — symmetric with Global | Small | Low |
| 9. Value type specs | Low — indirectly covered | Medium | Low |
