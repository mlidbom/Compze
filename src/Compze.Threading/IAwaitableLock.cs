namespace Compze.Threading;

///<summary>
/// A lock that supports both read and update semantics with condition-based waiting.<br/>
/// All locks are exclusive.<br/>
/// Update locks wake threads awaiting some condition becoming true such as <see cref="TakeReadLockWhen"/>, <see cref="TakeUpdateLockWhen"/>, <see cref="TryTakeReadLockWhen"/>, <see cref="TryTakeUpdateLockWhen"/> and default interface methods and extensions based on them.<br/>
/// Dispose the returned <see cref="IDisposable"/> to release the taken lock and, if it is an update lock, notify threads waiting for conditions to evaluate the conditions again.
/// </summary>
public partial interface IAwaitableLock : ILockInfo
{
   ///<summary>Acquires a read lock, blocking until available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> on timeout. Uses <see cref="ILockInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IDisposable TakeReadLock(LockTimeout? timeout = null);
   ///<summary>Acquires an update lock, blocking until available or <paramref name="timeout"/> expires. Throws <exception cref="Exceptions.TakeLockTimeoutException"/> on timeout. Uses <see cref="ILockInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   IDisposable TakeUpdateLock(LockTimeout? timeout = null);

   ///<summary>Blocks until <paramref name="condition"/>, then returns a read lock with <paramref name="condition"/> guaranteed to still be true.<br/> Throws <exception cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ILockInfo.LockTimeout"/> for null parameters.</summary>
   IDisposable TakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   ///<summary>Blocks until <paramref name="condition"/>, then returns an update lock with <paramref name="condition"/> guaranteed to still be true.<br/> Throws <exception cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ILockInfo.LockTimeout"/> for null parameters.</summary>
   IDisposable TakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ILockInfo.LockTimeout"/> for null parameters.</summary>
   IDisposable? TryTakeReadLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
   ///<summary>Blocks until <paramref name="condition"/> returns true then acquires an update lock. Returns null if <paramref name="waitTimeout"/> expires. Uses <see cref="WaitTimeout"/> and <see cref="ILockInfo.LockTimeout"/> for null parameters.</summary>
   IDisposable? TryTakeUpdateLockWhen(Func<bool> condition, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);

   ///<summary>The timeout used in condition-wait methods if no explicit wait timeout is provided.</summary>
   WaitTimeout WaitTimeout { get; }
}
