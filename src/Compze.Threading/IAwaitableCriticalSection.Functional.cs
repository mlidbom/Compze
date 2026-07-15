using System.Diagnostics.CodeAnalysis;
using Compze.SystemCE;

namespace Compze.Threading;

public partial interface IAwaitableCriticalSection
{
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   ///<summary>Acquires a read lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn Read<TReturn>(Func<TReturn> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null)
   {
      using(TakeReadLock(cancellationToken, timeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, then - within the same read lock held while executing <paramref name="condition"/> - executes <paramref name="func"/> and returns its result.</summary>
   TReturn ReadWhen<TReturn>(Func<bool> condition, Func<TReturn> func, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeReadLockWhen(condition, cancellationToken, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires.<br/>
   /// If it became true, executes <paramref name="func"/> - within the same read lock held while evaluating <paramref name="condition"/>, with <paramref name="condition"/> guaranteed still true - assigns its return value to <paramref name="result"/>, and returns true.<br/>
   /// If <paramref name="waitTimeout"/> expired, sets <paramref name="result"/> to its default and returns false without executing <paramref name="func"/>.<br/>
   /// The read counterpart of <see cref="TryUpdateWhen"/>. Unlike <see cref="TryAwait"/>, which releases the read lock before returning, this holds it across <paramref name="func"/> so the read observes the exact state that satisfied <paramref name="condition"/> - use it to gate an expensive <paramref name="func"/> on a cheaply-evaluated <paramref name="condition"/>.</summary>
   bool TryReadWhen<TReturn>(Func<bool> condition, Func<TReturn> func, [MaybeNullWhen(false)] out TReturn result, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var readLock = TryTakeReadLockWhen(condition, cancellationToken, waitTimeout, lockTimeout);
      if(readLock == null)
      {
         result = default;
         return false;
      }

      result = func();
      return true;
   }

   ///<summary>Executes <paramref name="action"/> within an update lock, notifying all waiting threads that there are updates.</summary>
   Unit Update(Action action, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Update(action.ToFunc(), cancellationToken, timeout);

   ///<summary>Executes <paramref name="func"/> within an update lock, notifying all waiting threads that there are updates.</summary>
   T Update<T>(Func<T> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null)
   {
      using(TakeUpdateLock(cancellationToken, timeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true, then upgrades its read lock to an update lock, and executes <paramref name="action"/> within that lock, notifying all waiting threads that there are updates.</summary>
   Unit UpdateWhen(Func<bool> condition, Action action, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
      UpdateWhen(condition, action.ToFunc(), cancellationToken, waitTimeout, lockTimeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true, then upgrades its read lock to an update lock, and executes <paramref name="func"/> within that lock, notifying all waiting threads that there are updates.</summary>
   TReturn UpdateWhen<TReturn>(Func<bool> condition, Func<TReturn> func, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using(TakeUpdateLockWhen(condition, cancellationToken, waitTimeout: waitTimeout, lockTimeout: lockTimeout)) return func();
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true or <paramref name="waitTimeout"/> expires, <br/>
   /// if <paramref name="waitTimeout"/> did not expire, upgrades its read lock to an update lock and executes <paramref name="action"/> -notifying all waiting threads that there are updates - and returns true. <br/>
   /// If <paramref name="waitTimeout"/> expired, returns false without doing anything.</summary>
   bool TryUpdateWhen(Func<bool> condition, Action action, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      using var updateLock = TryTakeUpdateLockWhen(condition, cancellationToken, waitTimeout, lockTimeout);
      if(updateLock == null) return false;
      action();
      return true;
   }
#pragma warning restore CA1068
}
