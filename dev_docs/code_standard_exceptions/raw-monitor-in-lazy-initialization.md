# A raw Monitor in lazy initialization

**The rule set aside:** "Always use the locking primitives in Compze.Threading. IMonitor, IAwaitableMonitor, IMutex etc.
NEVER raw lock, Monitor, Mutex etc."

**Where:** `DoubleCheckedLockingOnAnExistingObject` in Compze.Internals.SystemCE (`_private`), which is what
`LazyCE<TValue>` uses. Nowhere else.

**What it does instead:** double-checked locking that locks, with `Monitor.TryEnter` and `LockTimeout.Default`, on an
object the caller already has: a `LazyCE` locks on itself. The lock is taken only while the value is still being
created; reading it afterwards takes no lock.

**Why:** a `LazyCE` can exist in very large numbers, each created and read once. JAStudio's parser creates one per
candidate word, millions in a run. An `IMonitor` per instance cost ten allocations and 424 bytes, and creating an
instance and reading it once took 55 ns. Locking on the instance itself costs 32 bytes, one allocation, and 14 ns
(BenchmarkDotNet, .NET 10, x64, 2026-11-12). Allocating a lock object on first read instead
(`LazyInitializer.EnsureInitialized` with a `syncLock`) costs 64 bytes and 22 ns, and still uses a raw `Monitor`.

**What is lost, and how much of it is kept:**

- The Compze primitives' lock timeout is kept: a thread that waits longer than `LockTimeout.Default` for another
  thread's factory throws a `TimeoutException`, so a deadlock fails loud instead of hanging.
- The blocking thread's stack trace, which `TakeLockTimeoutException` adds to its message, is lost: that needs the
  holder to record its stack trace when it releases the lock, which is the machinery that costs the memory.
- `LazyCE` is public, so code outside it could lock on an instance and share its lock. The documentation of `LazyCE`
  says not to. The alternative, a private lock object, is the allocation this exception exists to avoid.

**Approved by:** Magnus, 2026-11-12.
