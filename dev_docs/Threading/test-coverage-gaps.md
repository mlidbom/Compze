# Test Coverage Gaps: Compze.Threading + Compze.InterprocessObject


---

## Gap 1: `LockTimeout` / `WaitTimeout` — no dedicated value type specs

These strongly-typed duration wrappers have factory methods, operators (`==`, `!=`), implicit conversions to `TimeSpan`, `ToString()`, and validation logic (e.g., `LockTimeout` rejects `InfiniteTimeSpan`). None of this is systematically tested. They're exercised indirectly through lock specs, but edge cases aren't covered:
- `LockTimeout` constructor throws on `Timeout.InfiniteTimeSpan`
- Equality operators
- Implicit `TimeSpan` conversions
- `WaitTimeout.Infinite` and `WaitTimeout.IsInfinite`
