# Interprocess Signaling Design

## Architecture

Three layers, bottom to top:

### `InterprocessChangeCounter` — shared MMF counter

A cross-process atomic counter backed by a memory-mapped file. Nanosecond reads, no syscall.

- File: `src/Compze.Threading.Interprocess/InterprocessChangeCounter.cs`
- Specs: `test/Compze.Threading.InternalSpecifications/Interprocess/InterprocessChangeCounter_specification.cs`
- Constructor takes a `FileInfo` backing file path
- `Increment()` — atomic `Interlocked.Increment` on an unsafe pointer into the MMF
- `Count` — atomic `Interlocked.Read` on the same pointer
- Counter is `long` (8 bytes), file-backed MMF on all platforms (single code path, no Windows-specific `CreateOrOpen`)
- Internal class — `InternalsVisibleTo` only to `Compze.Threading.InternalSpecifications`

### `InterprocessSignal` — signaling primitive

Wraps `InterprocessChangeCounter` with a polling wait API.

- File: `src/Compze.Threading.Interprocess/InterprocessSignal.cs`
- Constructor takes a name + `DirectoryInfo` + an optional `ISignalPollingPolicy`; creates backing file at `{directory}/{name}.signal`
- `Raise()` → `_counter.Increment()`
- `Snapshot()` → `_counter.Count` (returns baseline for `TryAwait`)
- `TryAwait(timeout, ref baseline[, waitStartedAt], cancellationToken)` — polls the counter on the schedule the `ISignalPollingPolicy` decides. Returns true + updates baseline when counter changes. Returns false on timeout. Sleeps on the cancellation token's wait handle, so cancellation wakes it instantly even mid-interval. The optional `waitStartedAt` lets a logical wait spanning several `TryAwait` calls keep backing off instead of restarting the schedule.

Each poll is a nanosecond memory read — no mutex acquisition, no syscall beyond the sleep. The cost that matters is the wakeup itself: frequent wakeups keep the CPU out of its deep low-power idle states (a real battery cost), which is why the default policy backs off — it sleeps a quarter of the wait so far, clamped between 1ms and a 50ms cap, so the cap is also the worst-case added signal-detection latency during a long wait.

### `IAwaitableMutex` / `AwaitableMutex` — condition-wait mutex

Wraps `IMutex` + `InterprocessSignal` to implement `IAwaitableCriticalSection`.

- Interface: `src/Compze.Threading.Interprocess/IAwaitableMutex.cs`
- Implementation: `src/Compze.Threading.Interprocess/IAwaitableMutex.AwaitableMutex.cs`
- Tested via `[IAwaitableCriticalSectionMatrix]` — runs against Monitor, GlobalMutex, LocalMutex

**Lock operations:**
- `TakeUpdateLock()` → acquires mutex, returns `UpdateLockDisposer` that raises the signal then releases the mutex on dispose
- `TakeReadLock()` → acquires mutex, returns lock directly (no signal on dispose)

**Condition wait (`TryTakeLockWhen`):**
1. Acquire mutex (reentrant if caller already holds it)
2. Check condition — if true, return lock
3. Snapshot signal baseline (while holding the lock — ensures no signals are missed)
4. Release ALL nesting levels via `IMutex.ReleaseAllNestingLevels()` (mirrors `Monitor.Wait()`)
5. Wait for signal: call `_signal.TryAwait` in chunks of at most `AbandonedMutexCheckInterval` (1 second), each clamped to the remaining wait time so a finite wait timeout is honored precisely
6. If `TryAwait` returns false (chunk elapsed without a signal) → probe mutex with zero-timeout `TryTakeLock` to detect abandoned mutexes, then loop back to step 5
7. If `TryAwait` returns true (signal detected) → reacquire to full nesting depth, go to step 2

**Two wakeup schedules, different purposes:**
- **`ISignalPollingPolicy`** (inside `InterprocessSignal.TryAwait`): decides each sleep between counter polls. The default backs off from 1ms at the start of a wait to a 50ms cap (a quarter of the elapsed wait, clamped), trading signal-detection latency against the power cost of frequent CPU wakeups. Detects normal `UpdateLock` releases.
- **1s** (`AbandonedMutexCheckInterval`, inside `AwaitableMutex.TryTakeLockWhen`): abandoned-mutex safety probe. Only runs when a whole chunk passed with no signal. Probes the mutex with zero-timeout to trigger `AbandonedMutexException` if the holder died. Kept long deliberately — abandonment is a rare crash scenario, and the probe is a wakeup floor on the power cost of a long wait.

**Abandoned mutex handling:**
- At construction, the user's `onAbandonedMutex` callback is wrapped: abandonment detection also calls `_signal.Raise()`
- When the once-per-second probe detects an abandoned mutex → callback fires → signal raised → `TryAwait` sees the counter change → condition re-evaluated

**Nesting-aware condition wait:**
- `MutexCE` tracks per-thread nesting depth via `ThreadLocal<int>`
- `IMutex.ReleaseAllNestingLevels()` releases all levels, returns the depth
- `IMutex.ReacquireToNestingDepth(depth)` restores the nesting state
- On timeout: restores caller's nesting (depth - 1) and returns null
- On exception: reacquires to full depth before rethrowing (mirrors `Monitor.Wait()`)

## Process death

- **Watcher dies**: no impact
- **All processes die**: counter resets or goes stale — harmless, worst case one spurious condition check
- **Signaler dies after modifying state but before signaling**: abandoned-mutex detection covers this. The once-per-second safety probe triggers `AbandonedMutexException` → wrapped callback raises the signal → watchers wake up and re-evaluate
