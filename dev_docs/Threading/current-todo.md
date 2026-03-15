# Threading — Current TODO

## Open Items

### Test coverage gaps (from `test-coverage-gaps.md`)

| Gap | Description | Priority |
|-----|-------------|----------|
| 4 | `CorruptionAction.ReplaceContentWithDefaultAndThrow` — untested error path in `IInterprocessObject` | Medium |
| 6 | `IProcessShared_specification` uses `[XF]` with manual Global/Local classes instead of the existing `[IProcessSharedMatrix]` attribute | Low |
| 8 | `IMutex` Local scope cross-instance synchronization untested (Global equivalent exists) | Low |
| 9 | `LockTimeout` / `WaitTimeout` value type edge cases not systematically tested | Low |

### Matrix coverage (from `test-matrix-coverage.md`)

All matrix attributes and specification files are implemented. One inconsistency remains:
- `IProcessShared_specification` should use `[IProcessSharedMatrix]` instead of manually duplicated `[XF]` Global/Local classes (Gap 7 in matrix doc)
