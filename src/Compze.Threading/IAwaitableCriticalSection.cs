namespace Compze.Threading;

///<summary>
/// A critical section that supports both read and update semantics with condition-based waiting.<br/>
/// Code declares what it does with the state the critical section protects: it reads the state, under a read lock, or it updates it, under an update lock.<br/>
/// Releasing an update lock wakes the waits for a condition, which evaluate their conditions again: <see cref="TakeReadLockWhen"/>, <see cref="TakeUpdateLockWhen"/>,<br/>
/// <see cref="TryTakeReadLockWhen"/>, <see cref="TryTakeUpdateLockWhen"/>, the async <see cref="TryAwaitAsync"/> and <see cref="TryUpdateWhenAsync"/>, and the default interface methods and extensions based on them.<br/>
/// Dispose the returned <see cref="IReadLock"/> or <see cref="IUpdateLock"/> to release the lock.
///</summary>
///<remarks>
/// Why reads and updates are declared apart: the declaration tells whoever reads the code, at each call site, whether the state changes there.<br/>
/// And only an update can make a condition true, so only releasing an update lock wakes the waits. Releasing a read lock wakes none, so reads cost the waits nothing.<br/>
/// That is what the declaration asks of the code: code under a read lock must not change the state. A change there wakes no wait, so a wait whose condition<br/>
/// it made true goes on waiting until the next update, or until its timeout, which by default is never (<see cref="Compze.Threading.WaitTimeout.Default"/>).<br/>
/// A condition must not change the state either: it runs under the lock, and evaluating it wakes no wait.
///</remarks>
///<remarks>
/// All locks are exclusive, read locks included: a read declares what the code does, not that it may run alongside other reads.<br/>
/// So a thread holding a read lock may take an update lock as well, as <see cref="AwaitableCriticalSectionDoubleCheckedLocking"/> does.
///</remarks>
///<remarks>
/// An async condition wait holds no thread while its condition is false: nothing waits but a task the next update-lock release completes.<br/>
/// That is what async code needs from a wait that can last seconds. A synchronous wait parks its thread for that long, and when<br/>
/// that thread is a thread-pool thread, enough such waits starve the pool: the work they wait for can't get a thread to run on.
///</remarks>
///<remarks>
/// Taking the lock is still synchronous. It waits only while another holder is inside one of the short critical sections this type<br/>
/// is for, bounded by <see cref="ICriticalSectionInfo.LockTimeout"/>, so it is never the long wait.<br/>
/// The condition, and the action a wait runs once the condition holds, run under the lock on whichever thread resumes the wait.<br/>
/// They are synchronous: nothing can await while holding the lock.<br/>
/// A thread that already holds this critical section can't start an async condition wait. A synchronous wait releases its caller's lock<br/>
/// while it waits and takes it back before it returns, but an async wait may resume on another thread, which can do neither. The wait<br/>
/// fails at once instead of waiting until its timeout for an update its own caller's lock prevents.
///</remarks>
public partial interface IAwaitableCriticalSection : ICriticalSectionInfo
{
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   ///<summary>Acquires a read lock, blocking until available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> on timeout. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.<br/>
   /// Code holding it must not change the state this critical section protects: releasing a read lock wakes no wait. <see cref="IAwaitableCriticalSection"/> tells why.</summary>
   IReadLock TakeReadLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);

   ///<summary>Acquires an update lock, blocking until available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> on timeout. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.<br/>
   /// Releasing it wakes the waits for a condition, so they evaluate their conditions again.</summary>
   IUpdateLock TakeUpdateLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);

   ///<summary>Blocks until <paramref name="condition"/>, then returns a read lock with <paramref name="condition"/> guaranteed to still be true.<br/> Throws <exception cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.<br/>
   /// Code holding the read lock must not change the state this critical section protects, and neither may <paramref name="condition"/>: see <see cref="IAwaitableCriticalSection"/>.</summary>
   IReadLock TakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Blocks until <paramref name="condition"/>, then returns an update lock with <paramref name="condition"/> guaranteed to still be true.<br/> Throws <exception cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true, then returns a read lock with <paramref name="condition"/> guaranteed to still be true. Returns null if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.<br/>
   /// Code holding the read lock must not change the state this critical section protects, and neither may <paramref name="condition"/>: see <see cref="IAwaitableCriticalSection"/>.</summary>
   IReadLock? TryTakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true then acquires an update lock. Returns null if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   IUpdateLock? TryTakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Awaits until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires, holding no thread while it waits.<br/>
   /// Returns true if <paramref name="condition"/> became true, false if <paramref name="waitTimeout"/> expired.<br/>
   /// The async counterpart of <see cref="TryAwait"/>. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   ///<remarks><paramref name="condition"/> is evaluated under a read lock when the wait starts, and again after every update-lock release until it holds.<br/>
   /// <paramref name="cancellationToken"/> ends the wait with an <see cref="OperationCanceledException"/>.<br/>
   /// See <see cref="IAwaitableCriticalSection"/> for how async condition waits work, and why the thread starting one must not hold this critical section.</remarks>
   Task<bool> TryAwaitAsync(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Awaits until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires, holding no thread while it waits.<br/>
   /// If <paramref name="condition"/> became true, executes <paramref name="action"/> within the update lock held while evaluating it, so <paramref name="condition"/> is<br/>
   /// guaranteed still true, then releases the lock, notifying all waits that there are updates, and returns true.<br/>
   /// If <paramref name="waitTimeout"/> expired, returns false without executing <paramref name="action"/>.<br/>
   /// The async counterpart of <see cref="TryUpdateWhen(Func{bool}, Action, CancellationToken, Compze.Threading.WaitTimeout?, Compze.Threading.LockTimeout?)"/>. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   ///<remarks>Only an evaluation that finds <paramref name="condition"/> true leads to an update; one that finds it false releases the lock without waking anything.<br/>
   /// <paramref name="cancellationToken"/> ends the wait with an <see cref="OperationCanceledException"/>.<br/>
   /// See <see cref="IAwaitableCriticalSection"/> for how async condition waits work, and why the thread starting one must not hold this critical section.</remarks>
   Task<bool> TryUpdateWhenAsync(Func<bool> condition, Action action, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
#pragma warning restore CA1068

   ///<summary>The timeout used in condition-wait methods if no explicit wait timeout is provided.</summary>
   WaitTimeout WaitTimeout { get; }

   //rationale: No shared reads. Read locks are exclusive, though a read lock that lets several readers in at once is possible. It was weighed
   //(Magnus, 2026-11-14) and left out, until a profile shows read contention on a monitor, and then for a type of its own, for these reasons:
   //- Condition waits would need machinery of their own. Monitor.Wait and Monitor.PulseAll work only with the exclusive Monitor, so shared
   //  reads need another way to wait, synchronous and async: a rewrite of the core.
   //- An update nested in a read deadlocks once two readers try it at the same time, each waiting for the other to leave. Today the nesting
   //  works, since the lock is re-entrant, and AwaitableCriticalSectionDoubleCheckedLocking relies on it. Shared reads would have to forbid
   //  it, with an assertion that fails at once, to stay safe by default.
   //- The deadlock diagnosis names the thread that blocked the one whose lock timed out. With shared reads, that can be several threads.
   //- IAwaitableMutex can't share: a system Mutex is exclusive. Shared reads on this interface, which it implements, would be exclusive
   //  there, and the name would lie. So they belong on an in-process type of their own.
   //- They pay off only for long, contended reads. For the short critical sections this library is for, a reader-writer lock generally
   //  costs more than Monitor.
}
