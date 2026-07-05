# Compze.Threading.Interprocess

Cross-process synchronization for [Compze.Threading](../Compze.Threading/). It brings the same pit-of-success
locking model — impossible to forget to lock, automatic deadlock diagnosis with dual stack traces — across
process boundaries, backed by the operating system's named mutexes.

Extracted from Compze.Threading 0.7.0-alpha so the cross-process primitives can version independently of the
in-process ones. It depends on Compze.Threading for the shared `ICriticalSection` / `IAwaitableCriticalSection`
abstractions that both the in-process and cross-process implementations satisfy — so an `IMutex` is an
`ICriticalSection` and an `IAwaitableMutex` is an `IAwaitableCriticalSection`, interchangeable with their
in-process counterparts wherever the abstraction is what matters.

## What it adds

### `IMutex` — a cross-process lock that diagnoses its own deadlocks

A named system `Mutex` wrapped so it behaves like the rest of Compze.Threading: `Global` (visible across all
login sessions) and `Local` (one session) variants, cancellation-token support, abandoned-mutex recovery, and
the same timeout-and-dual-stack-trace deadlock diagnostics as the in-process `IMonitor`.

### `IAwaitableMutex` — `Monitor.Wait` semantics across processes

The full condition-wait model — `TakeUpdateLockWhen(condition)`, `TakeReadLockWhen(condition)`, and the
`TryTake*` variants — working across processes. It releases every nesting level of the lock while waiting and
reacquires on signal, mirroring `Monitor.Wait` exactly. As far as we know, no other .NET library exposes the
full Monitor/condition-wait model over a cross-process mutex.

```csharp
using var mutex = IAwaitableMutex.Global("MyResource", directory);
using(mutex.TakeUpdateLockWhen(() => ResourceIsReady()))
{
    // Exclusive across every process on the machine; the wait released the lock
    // for other processes and reacquired it once the condition held.
}
```

### `IProcessShared<T>` / `IAwaitableProcessShared<T>`

Pair a shared object with a cross-process mutex to coordinate access to an external resource — a file, a port,
a database — across processes. (For genuine cross-process shared *state*, see
[Compze.InterprocessObject](../Compze.InterprocessObject/), which builds on this package.)

### `ISignalPollingPolicy` — the latency-versus-power knob

Awaiting a cross-process condition polls a memory-mapped change counter, because .NET has no portable
cross-process kernel signal. Each poll wakes the CPU, and frequent wakeups keep it out of its deep low-power
idle states — a real battery cost on laptops. `ISignalPollingPolicy` decides the poll cadence; the default
backs off adaptively (eager at first, then stretching toward a 50 ms cap), and you can supply your own.
