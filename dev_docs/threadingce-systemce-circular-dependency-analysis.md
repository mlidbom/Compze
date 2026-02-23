# ThreadingCE ↔ SystemCE Circular Dependency Analysis

## Current State

- **SystemCE → ThreadingCE**: Normal `ProjectReference`. SystemCE uses `IMonitorCE`, `IThreadShared`, `TaskCE`, `ConfigureAwaitCE`, and other types exported by ThreadingCE.
- **ThreadingCE → SystemCE**: `InternalizeSourceFrom` in the csproj still copies **all** of SystemCE's source, rewrites `public` to `internal`, and compiles it as ThreadingCE's private copy. The goal is to eliminate this entirely.

## What ThreadingCE Internalizes

ThreadingCE's csproj has `<InternalizeSourceFrom>..\Compze.Utilities.SystemCE</InternalizeSourceFrom>`, which copies **all ~70 source files** from SystemCE. Of ThreadingCE's **18 own source files**, only **3 actually use internalized SystemCE types**:

### `IMonitorCE.cs` — uses:
| Internalized Type | API |
|---|---|
| `CompzeEnvironment` | `CompzeEnvironment.IsNCrunch` (compile-time constant) |
| `TimeSpanCE.FluentFactory` | `.Seconds()`, `.Minutes()` |

### `IMonitorCE.MonitorCE.cs` — uses:
| Internalized Type | API |
|---|---|
| `Disposable` | `new Disposable(action)` |
| `TimeSpanCE.FluentFactory` | `.Seconds()`, `.Milliseconds()` |
| `TimeSpanCE` | `.None()` |
| `DateTimeCE` | `DateTimeCE.TimeElapsedSince()` |

### `ThreadPoolCE.cs` — uses:
| Internalized Type | API |
|---|---|
| `LinqCE/EnumerableCE.IntSequenceGeneration` | `int.Through(int)` |