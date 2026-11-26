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

The async condition waits — `TryAwaitAsync`, `ReadWhenAsync`, `UpdateWhenAsync`, `TryUpdateWhenAsync` — work across processes
too, and hold no thread while they wait: between polls of the signal (see `ISignalPollingPolicy` below) nothing waits but a timer.

### `IProcessLock` — a lock between processes that any thread can release

A lock on an `IMutex` can only be released by the thread that took it, so async code can't hold it across an await,
nor can an xUnit fixture take it as it is created and release it as it is disposed. An `IProcessLock` belongs to its
holder instead: disposing the `IProcessLockHold` that taking it returns releases it, from whichever thread. The
operating system releases it when the holder's process dies, so a crashed or killed process never leaves it held.

```csharp
var sandboxTurn = IProcessLock.New("sandbox-turn", directory);
using(await sandboxTurn.TakeAsync("running the sandbox specifications", WaitTimeout.Minutes(20)))
{
    // Every other process taking "sandbox-turn" in this directory waits until the hold is disposed.
}
```

The wait is a `WaitTimeout`, not a `LockTimeout`: a holder may rightly hold it for minutes, so a long wait is no sign
of a deadlock. A `Take` that gives up says what the lock is held for, by which process, and since when;
`TryTake(purpose, WaitTimeout.Zero)` answers at once. The lock is a file in the directory, which its holder keeps open,
and a wait tries it every 50 milliseconds: it isn't fair, nor reentrant.

### `IProcessSemaphore` — a counting semaphore between processes that a dead holder can't leave held

Of its units, such as sandboxes that may run at once, each is held by one holder at a time, whichever process it is in.
A take of several units takes them all or none, and takes are served in the order they came, so a take of many units
isn't starved by takes of few. Each unit is held as an `IProcessLock` is, so the operating system releases the units of
a holder whose process dies, and a take whose process dies waiting gives up its place. Windows' named `Semaphore` has
no holders: units a dead process held are gone until it is made anew.

```csharp
var sandboxes = IProcessSemaphore.New("sandboxes", units: 16, directory);
using(await sandboxes.TakeAsync(3, "running the scenario's three sandboxes", WaitTimeout.Minutes(20)))
{
    // At most 16 sandboxes run at once, whichever processes on the machine run them.
}
```

A `Take` that gives up says who holds the units and who waits ahead: how many units, what for, which process, and
since when. `DescribeHoldersAndWaiters()` says the same while a take waits, which giving up and taking again would lose
its place in the order.

### `IProcessShared<T>` / `IAwaitableProcessShared<T>`

Pair a shared object with a cross-process mutex to coordinate access to an external resource — a file, a port,
a database — across processes. Each process has its own object: for an object the processes really share, see
`IInterprocessObject<T>` below.

### `IInterprocessObject<T>` — an object shared between processes, in a single line of code

An `IAwaitableProcessShared<T>` whose object every process shares: kept in a memory-mapped file, so a change one
process makes is visible to all the others at once, and the object survives the processes and reboots.

- **Atomic updates:** every read and update holds the object's cross-process mutex, so a read-modify-write is safe.
- **Waits for conditions:** the same condition waits as `IAwaitableMutex`, with the object loaded for each evaluation
  of the condition and saved after an update.
- **Recovery from corruption:** `CorruptionAction` says whether an object that can't be read is replaced with its
  default or left as it is for you to look at. Either way the access that found it throws, saying what happened.
- **Pluggable serialization:** JSON with `IInterprocessObjectSerializer.Json<T>()`, MemoryPack with the
  [Compze.Threading.Interprocess.MemoryPack](https://www.nuget.org/packages/Compze.Threading.Interprocess.MemoryPack) plugin,
  or a serializer of your own.

```csharp
using var visits = IInterprocessObject.NewGlobal("visits", IInterprocessObjectSerializer.Json<Visits>(), () => new Visits(),
                                                 CorruptionAction.ReplaceContentWithDefaultAndThrow, maxBytes: 4 * 1024, directory);
visits.Update(it => it.Count++);
```

It came here from Compze.InterprocessObject, which is retired: its types are in the namespace
`Compze.Threading.Interprocess.ResourceAccess` now.

### `ISignalPollingPolicy` — the latency-versus-power knob

Awaiting a cross-process condition polls a memory-mapped change counter, because .NET has no portable
cross-process kernel signal. Each poll wakes the CPU, and frequent wakeups keep it out of its deep low-power
idle states — a real battery cost on laptops. `ISignalPollingPolicy` decides the poll cadence; the default
backs off adaptively (eager at first, then stretching toward a 50 ms cap), and you can supply your own.
