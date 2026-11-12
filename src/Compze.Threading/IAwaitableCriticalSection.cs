namespace Compze.Threading;

///<summary>
/// A critical section that supports both read and update semantics with condition-based waiting.<br/>
/// All locks are exclusive.<br/>
/// Update locks wake threads awaiting some condition becoming true such as <see cref="TakeReadLockWhen"/>, <see cref="TakeUpdateLockWhen"/>, <see cref="TryTakeReadLockWhen"/>, <see cref="TryTakeUpdateLockWhen"/> and default interface methods and extensions based on them.<br/>
/// They wake the async condition waits too: <see cref="TryAwaitAsync"/>, <see cref="TryUpdateWhenAsync"/> and the default interface methods based on them.<br/>
/// Dispose the returned <see cref="IReadLock"/> or <see cref="IUpdateLock"/> to release the taken lock and, if it is an update lock, notify threads waiting for conditions to evaluate the conditions again.
/// </summary>
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
   ///<summary>Acquires a read lock, blocking until available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> on timeout. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IReadLock TakeReadLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);

   ///<summary>Acquires an update lock, blocking until available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> on timeout. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IUpdateLock TakeUpdateLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);

   ///<summary>Blocks until <paramref name="condition"/>, then returns a read lock with <paramref name="condition"/> guaranteed to still be true.<br/> Throws <exception cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   IReadLock TakeReadLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Blocks until <paramref name="condition"/>, then returns an update lock with <paramref name="condition"/> guaranteed to still be true.<br/> Throws <exception cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
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
   /// The async counterpart of <see cref="TryUpdateWhen"/>. Uses <see cref="WaitTimeout"/> and <see cref="ICriticalSectionInfo.LockTimeout"/> for null parameters.</summary>
   ///<remarks>Only an evaluation that finds <paramref name="condition"/> true leads to an update; one that finds it false releases the lock without waking anything.<br/>
   /// <paramref name="cancellationToken"/> ends the wait with an <see cref="OperationCanceledException"/>.<br/>
   /// See <see cref="IAwaitableCriticalSection"/> for how async condition waits work, and why the thread starting one must not hold this critical section.</remarks>
   Task<bool> TryUpdateWhenAsync(Func<bool> condition, Action action, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
#pragma warning restore CA1068

   ///<summary>The timeout used in condition-wait methods if no explicit wait timeout is provided.</summary>
   WaitTimeout WaitTimeout { get; }
}
