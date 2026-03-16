namespace Compze.Threading;

///<summary>
/// A critical section that supports both read and update semantics with condition-based waiting.<br/>
/// All locks are exclusive.<br/>
/// Update locks wake threads awaiting some condition becoming true such as <see cref="TakeReadLockWhen"/>, <see cref="TakeUpdateLockWhen"/>, <see cref="TryTakeReadLockWhen"/>, <see cref="TryTakeUpdateLockWhen"/> and default interface methods and extensions based on them.<br/>
/// Dispose the returned <see cref="IReadLock"/> or <see cref="IUpdateLock"/> to release the taken lock and, if it is an update lock, notify threads waiting for conditions to evaluate the conditions again.
/// </summary>
public partial interface IAwaitableCriticalSection : ICriticalSectionInfo
{
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

   ///<summary>The timeout used in condition-wait methods if no explicit wait timeout is provided.</summary>
   WaitTimeout WaitTimeout { get; }
}
