# A raw Monitor in the tevent dispatcher

**The rule set aside:** "Always use the locking primitives in Compze.Threading. IMonitor, IAwaitableMonitor, IMutex etc.
NEVER raw lock, Monitor, Mutex etc."

**Where:** `CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent>.Subscriptions` in Compze.Teventive (`_private`),
the implementation of `IMutableTeventDispatcher<TTevent>`. Nowhere else.

**What it does instead:** a dispatch of a tevent whose wrapper type it has already resolved takes no lock at all: the
resolved handlers are a `Dictionary` that is never changed once published, read with `Volatile.Read`. Resolving another
type publishes a copy with that type added, and a change of subscriptions publishes an empty one. Those, and every change
of subscriptions, hold a lock taken with `Monitor.TryEnter` and `LockTimeout.Default` on the `Subscriptions` instance
itself, held through a `ref struct` that releases it when disposed. No handler runs while it is held.

**Why:** the dispatcher is on the hottest path of Compze: every tevent a taggregate, a component or a query model applies,
and every tevent an application publishes on a dispatcher of its own, goes through it, and every taggregate creates
dispatchers of its own each time it is loaded. Measured with BenchmarkDotNet (.NET 10, x64, Release, 2026-11-13):

| | Before it was thread-safe | Through `IThreadShared` | This |
|---|---|---|---|
| Dispatching an already wrapped tevent | 10 ns, 0 B | 43 ns, 184 B | 8 ns, 0 B |
| Creating a dispatcher and registering five handlers | 300 ns, 1,312 B | 593 ns, 3,360 B | 400 ns, 1,168 B |
| That, plus dispatching a history of twenty tevents of five types | 1,460 ns, 5,096 B | 2,573 ns, 10,744 B | 1,740 ns, 5,792 B |

Through `IThreadShared`, every dispatch took the lock, and `IShared.Locked` allocated two closures each time; each
dispatcher also allocated an `IMonitor`, ten allocations of its own.

**What is lost, and how much of it is kept:**

- The Compze primitives' lock timeout is kept: a thread that waits longer than `LockTimeout.Default` throws a
  `TimeoutException`. No consumer code runs while the lock is held, so it is not expected to happen.
- The blocking thread's stack trace, which `TakeLockTimeoutException` adds to its message, is lost.
- `CA2002`, "Do not lock on objects with weak identity", is suppressed where the lock is taken: `Subscriptions` is private
  to the dispatcher, so no code outside it can reach an instance to lock on it.

The same raw-`Monitor`-with-timeout pattern is in `DoubleCheckedLockingOnAnExistingObject`, for `LazyCE` (see
`raw-monitor-in-lazy-initialization.md`). If a third place needs it, it belongs in Compze.Threading as a primitive of its
own: a lock on an object the caller already has, allocating nothing.

**Approved by:** Magnus, 2026-11-13, as the way for an ultra-hot path such as this to depart from the locking primitives.
