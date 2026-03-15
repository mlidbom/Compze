# Interprocess Signaling via Memory-Mapped File

## Goal

Replace the polling-based `IPollingAwaitableMutex` (where user condition code runs every poll interval) with a signal-based approach where user condition code only runs when a state change is actually signaled.

## Mechanism

- A shared `MemoryMappedFile` holds a monotonically increasing `int` counter.
- **Signaler**: increments the counter (while still holding the mutex, before releasing it).
- **Watchers**: poll the MMF counter (nanosecond memory read, no syscall, no kernel transition). Only when the counter changes do they acquire the mutex and evaluate the user's condition.

This moves polling from "acquire lock + run expensive user condition every interval" to "read a single cache line every interval" — orders of magnitude cheaper.

## Shared Change Counter: `InterprocessChangeCounter` (IMPLEMENTED)

Internal class in `src/Compze.Threading/Interprocess/InterprocessChangeCounter.cs`
Specs: `test/Compze.Threading.InternalSpecifications/Interprocess/InterprocessChangeCounter_specification.cs`

This is NOT a signaling primitive — it's a shared counter used as a building block by the awaitable mutex. The mutex owns all polling and waiting logic; this class only provides cheap cross-process change detection.

API surface:
- `new InterprocessChangeCounter(name, global)` — constructor, named with Global/Local prefix matching `IMutex` conventions
- `Increment()` — atomically increments the counter (called by lock holder after modifying state)
- `Count` — reads the current counter (nanosecond memory read, no syscall). Consumers compare against a previously observed value to detect changes.
- `IsGlobal`, `Name` — identity properties
- `IDisposable` — cleanup of MMF handles

### Implementation details
- Internal class (not public API) — `InternalsVisibleTo` only to `Compze.Threading.InternalSpecifications`
- Thread-safe: uses `Interlocked.Increment`/`Read` on an unsafe pointer into the MMF — no external locking needed
- Counter is `long` (8 bytes)
- File-backed `MemoryMappedFile` on all platforms (single code path)
- Backing files: `{TempPath}/Compze/Signals/{Global_name}` or `{TempPath}/Compze/Signals/{Local_name}`
- Backing files persist across process restarts — counter values accumulate. Consumers must use delta comparison, not absolute values
- Name validation: rejects null/empty/whitespace and backslashes (same as `IMutex`)

## Cross-Platform Implementation

Use **file-backed `MemoryMappedFile` on all platforms** (including Windows). No platform-conditional code.

- Windows supports `CreateOrOpen` with just a name (no backing file), but using that would mean two code paths for no real gain.
- File-backed MMF reads still hit the page cache, not disk. Performance is identical for a 4-byte counter.
- Single code path = fewer bugs, easier testing, no platform-conditional behavior.

### File path
- Derive a deterministic file path from the shared name (e.g., temp directory + name-based filename)

### Creation
- Use atomic file creation (create-if-not-exists)
- If the file already exists with a stale counter, that's harmless — worst case is one spurious condition check

### Cleanup
- Backing file in temp directory
- Files are 4 bytes — negligible. Accept that temp files accumulate and get cleaned by OS, or clean up on dispose if possible.

## Process Death

### Why it's mostly a non-issue for the MMF counter
- **Watcher dies**: Zero impact. It just stops reading.
- **All processes die**: Counter resets or goes stale — harmless, worst case is one spurious condition check on restart.
- **Signaler dies mid-signal**: See below.

### IMPORTANT: Abandoned mutex = implicit signal

If the signaler dies **after modifying shared state but before incrementing the counter**, watchers would never wake up.

**Solution**: Signal must be incremented **while still holding the mutex, before releasing it.** This makes "signaler dies while holding lock" an abandoned-mutex scenario. When the next process detects the abandoned mutex, it **must treat that as an implicit signal** (i.e., behave as if the counter was incremented).

This is critical — without it, a process crash between state modification and signaling creates a silent missed-update bug.

## Integration with Awaitable Mutex: `ISignalingAwaitableMutex` (IMPLEMENTED)

Interface: `src/Compze.Threading/Interprocess/ISignalingAwaitableMutex.cs`
Implementation: `src/Compze.Threading/Interprocess/ISignalingAwaitableMutex.SignalingAwaitableMutexCE.cs`
Tested via: `[IAwaitableCriticalSectionMatrix]` attribute — all existing `IAwaitableCriticalSection` specs now run against Monitor, PollingMutex, *and* SignalingMutex

### How it works
- Wraps an `IMutex` + `InterprocessChangeCounter` with the same name
- `TakeUpdateLock()` returns an `UpdateLockDisposer` that increments the change counter *before* releasing the mutex
- `TakeReadLock()` returns the plain mutex lock (no counter increment)
- Wait loop (`TryTakeLockWhen`): snapshot counter → acquire mutex → check condition → if false, release → poll counter until changed → repeat
- The user condition only runs when an update lock was released somewhere — never on a blind timer

### Two-tier polling
- **Fast counter poll** (1ms): essentially free. Reads the MMF counter — nanosecond memory read, detects normal update lock releases
- **Safety probe** (50ms): if the counter hasn't changed for 50ms, probes the mutex with a zero-timeout `TakeLock`:
  - **Abandoned mutex**: `AbandonedMutexException` fires → wrapped `onAbandonedMutex` callback increments the counter → next 1ms poll sees the change → outer loop acquires mutex and evaluates the condition
  - **Mutex available (not abandoned)**: acquired and immediately released, no condition check, back to counter polling
  - **Mutex held by another process**: `TakeLockTimeoutException` caught and swallowed — holder is alive, back to counter polling
- The user condition **never** runs on the safety interval — only when the counter actually changes

### Abandoned mutex handling
- The user's `onAbandonedMutex` callback is wrapped at construction: abandonment detection also increments the `InterprocessChangeCounter`
- This means any code path that detects abandonment (safety probe, or normal `TakeLock` in the outer loop) triggers a counter change
- The counter change wakes the fast-polling loop, which then runs the normal condition-check path

### Design decisions
- Added `SignalingMutex` to `AwaitableCriticalSectionImplementation` enum — all `[IAwaitableCriticalSectionMatrix]` tests automatically cover it
- Counter polling interval is 1ms, since the counter read is a nanosecond memory read

## User condition only runs on updates

The user's condition function (likely involving IO, deserialization, etc.) must **only** execute when a signal has been raised — never on a polling interval. The only "polling" is reading the MMF counter, which is a direct memory read with negligible cost.
