# Compze.Utilities.SystemCE.ThreadingCE

Threading and synchronization utilities for [Compze](https://github.com/mlidbom/Compze).

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

Thread synchronization primitives, usage guards, async locks, Task utilities, and a rich testing toolkit for deterministic multi-threaded tests.

### Monitor (`IMonitorCE`)

A feature-rich monitor with read/update locks, conditional awaiting, and deadlock detection:

```csharp
var monitor = MonitorCE.WithTimeout(1.Seconds());

// Read lock
var value = monitor.Read(() => _sharedState);

// Update lock
monitor.Update(() => _sharedState = newValue);

// Conditional await — blocks until condition is true
monitor.Await(() => _queue.Count > 0);
```

### Usage guards

Catch threading violations at runtime:

- `SingleThreadUseGuard` — ensures component is accessed from one thread only
- `SingleTransactionUsageGuard` — ensures component stays within one transaction
- `CombinationUsageGuard` — composes multiple guards

### Async lock

```csharp
var asyncLock = new AsyncLockCE();
await using(await asyncLock.LockedAsync())
{
    // Critical section
}
```

### Task utilities

- `TaskCE.Run()` — guaranteed execution on a different thread
- `TaskCE.RunOnDedicatedThread()` — runs on a new dedicated thread
- `caf()` — abbreviated `ConfigureAwait(false)` for `Task`, `Task<T>`, `ValueTask`
- `WaitUnwrappingException()`, `ResultUnwrappingException()` — clean exception propagation

### Thread-safe shared state

- `IThreadShared<T>` — read/update/await operations on shared data
- `MachineWideSharedObject<T>` — cross-process shared state via files + named mutex

### Testing toolkit

Deterministic multi-threaded testing primitives:

- `ThreadGate` — controllable gate to synchronize threads at specific points
- `GatedCodeSection` — entrance + exit gates for controlling thread flow through code sections
- `TestingTaskRunner` — run background tasks with automatic exception propagation

```csharp
var gate = ThreadGate.WithTimeout(1.Seconds());
gate.Close();

// Start background work that will block at the gate
var task = Task.Run(() => { gate.AwaitPassThrough(); DoWork(); });

gate.AwaitQueueLengthEqualTo(1); // Wait for thread to arrive
gate.Open();                      // Let it through
```

## Installation

```shell
dotnet add package Compze.Utilities.SystemCE.ThreadingCE
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities.SystemCE](https://www.nuget.org/packages/Compze.Utilities.SystemCE) | System type extensions |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions |
| [Compze.Underscore](https://www.nuget.org/packages/Compze.Underscore) | Functional programming primitives |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |

## License

Apache-2.0
