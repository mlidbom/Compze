# Interprocess Signaling via Memory-Mapped File

## Goal

Replace the polling-based `IPollingAwaitableMutex` (where user condition code runs every poll interval) with a signal-based approach where user condition code only runs when a state change is actually signaled.

## Mechanism

- A shared `MemoryMappedFile` holds a monotonically increasing `int` counter.
- **Signaler**: increments the counter (while still holding the mutex, before releasing it).
- **Watchers**: poll the MMF counter (nanosecond memory read, no syscall, no kernel transition). Only when the counter changes do they acquire the mutex and evaluate the user's condition.

This moves polling from "acquire lock + run expensive user condition every interval" to "read a single cache line every interval" — orders of magnitude cheaper.

## Standalone Primitive: `IInterprocessSignal`

Build and test the signaling mechanism in isolation before integrating with the awaitable mutex.

API surface:
- `Signal()` — increments the counter
- `Version` / `CurrentSignalCount` — reads the current counter value
- `IDisposable` — cleanup of handles/files
- Named, with Global/Local variants (matching `IMutex` conventions)

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

## Integration with Awaitable Mutex

Once `IInterprocessSignal` is proven:
- The awaitable mutex's lock release increments the signal counter when the holder performed an update
- Watchers loop: read MMF counter → if changed, acquire mutex → evaluate condition → if not met, continue waiting
- Abandoned mutex detection triggers an implicit signal (watcher evaluates condition despite no counter change)

## User condition only runs on updates

The user's condition function (likely involving IO, deserialization, etc.) must **only** execute when a signal has been raised — never on a polling interval. The only "polling" is reading the MMF counter, which is a direct memory read with negligible cost.
