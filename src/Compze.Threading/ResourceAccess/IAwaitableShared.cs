using System.Diagnostics.CodeAnalysis;
using Compze.SystemCE;

namespace Compze.Threading.ResourceAccess;

///<summary>Provides lock-protected access to a shared object of type <typeparamref name="TShared"/> with read/update semantics and condition-based waiting.<br/>
/// Code declares what it does with the shared object: it reads it, with <see cref="Read{TResult}"/> and the other read methods,<br/>
/// or it updates it, with <see cref="Update{TResult}"/> and the other update methods. Only an update wakes the waits for a condition.</summary>
///<remarks>A read must not change the shared object, and neither may a condition: a change made there wakes no wait, so a wait whose<br/>
/// condition it made true goes on waiting until the next update, or until its timeout, which by default is never.<br/>
/// <see cref="IAwaitableCriticalSection"/> tells why reads and updates are declared apart, and that all locks are exclusive, read locks included.</remarks>
public interface IAwaitableShared<out TShared>
{
   ///<summary>The underlying critical section used to protect access.</summary>
   IAwaitableCriticalSection CriticalSection { get; }

#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   //core
   ///<summary>Acquires a read lock, passes the shared object to <paramref name="read"/>, returns the result, then releases the lock.<br/>
   /// <paramref name="read"/> must not change the shared object: a read wakes no wait. <see cref="IAwaitableShared{TShared}"/> tells why.</summary>
   TResult Read<TResult>(Func<TShared, TResult> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null);
   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires a read lock, executes <paramref name="read"/>, and returns its result.<br/>
   /// <paramref name="read"/> must not change the shared object, and neither may <paramref name="condition"/>: a read wakes no wait. <see cref="IAwaitableShared{TShared}"/> tells why.</summary>
   TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);
   ///<summary>Acquires an update lock, passes the shared object to <paramref name="update"/>, returns the result, then releases the lock.</summary>
   TResult Update<TResult>(Func<TShared, TResult> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null);
   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock, executes <paramref name="update"/>, and returns its result.</summary>
   TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   //Default implementations
   ///<summary>Acquires a read lock, passes the shared object to <paramref name="read"/>, then releases the lock.<br/>
   /// <paramref name="read"/> must not change the shared object: a read wakes no wait. <see cref="IAwaitableShared{TShared}"/> tells why.</summary>
   Unit Read(Action<TShared> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Read(read.ToFunc(), cancellationToken, timeout);

   ///<summary>Acquires an update lock, passes the shared object to <paramref name="update"/>, then releases the lock.</summary>
   Unit Update(Action<TShared> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Update(update.ToFunc(), cancellationToken, timeout);
   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock and executes <paramref name="update"/>.</summary>
   Unit UpdateWhen(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) => UpdateWhen(condition, update.ToFunc(), cancellationToken, timeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires.</summary>
   Unit Await(Func<TShared, bool> condition, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) => ReadWhen(condition, _ => unit, cancellationToken, timeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires.<br/>
   /// If it became true, executes <paramref name="read"/> on the shared object within the read lock held while <paramref name="condition"/> was evaluated - with <paramref name="condition"/> guaranteed still true - assigns its return value to <paramref name="result"/>, and returns true.<br/>
   /// If <paramref name="timeout"/> expired, sets <paramref name="result"/> to its default and returns false without executing <paramref name="read"/>.<br/>
   /// The read counterpart of <see cref="TryUpdateWhen{TResult}"/>. Unlike <see cref="TryAwait"/>, which releases the lock before returning, it holds the lock across <paramref name="read"/> so the read observes the exact state that satisfied <paramref name="condition"/> - use it to gate an expensive <paramref name="read"/> on a cheaply-evaluated <paramref name="condition"/>.<br/>
   /// <paramref name="read"/> must not change the shared object, and neither may <paramref name="condition"/>: a read wakes no wait. <see cref="IAwaitableShared{TShared}"/> tells why.</summary>
   bool TryReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, [MaybeNullWhen(false)] out TResult result, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock and executes <paramref name="update"/>. Returns false if the wait times out, else true.</summary>
   bool TryUpdateWhen(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires.<br/>
   /// If it became true, executes <paramref name="update"/> on the shared object within the update lock held while <paramref name="condition"/> was evaluated - with <paramref name="condition"/> guaranteed still true -<br/>
   /// assigns its return value to <paramref name="result"/>, releases the lock, waking the waits for a condition, and returns true.<br/>
   /// If <paramref name="timeout"/> expired, sets <paramref name="result"/> to its default and returns false without executing <paramref name="update"/>.<br/>
   /// The update counterpart of <see cref="TryReadWhen{TResult}"/>.</summary>
   bool TryUpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, [MaybeNullWhen(false)] out TResult result, CancellationToken cancellationToken = default, WaitTimeout? timeout = null)
   {
      TResult updateResult = default!;
      var updated = TryUpdateWhen(condition, shared => updateResult = update(shared), cancellationToken, timeout);
      result = updateResult;
      return updated;
   }

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires. Returns false if the wait times out, else true.<br/>
   /// The pure wait: unlike <see cref="TryUpdateWhen(Func{TShared, bool}, Action{TShared}, CancellationToken, Nullable{WaitTimeout})"/> nothing is written when the condition passes, so waiters observing the shared object never wake each other.</summary>
   bool TryAwait(Func<TShared, bool> condition, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   //async core
   ///<summary>Awaits until <paramref name="condition"/> returns true for the shared object, holding no thread while it waits, then executes <paramref name="read"/><br/>
   /// within the read lock held while evaluating <paramref name="condition"/> and returns its result. Throws <see cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="timeout"/> expires.<br/>
   /// The async counterpart of <see cref="ReadWhen{TResult}"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.<br/>
   /// <paramref name="read"/> must not change the shared object, and neither may <paramref name="condition"/>: a read wakes no wait. <see cref="IAwaitableShared{TShared}"/> tells why.</summary>
   Task<TResult> ReadWhenAsync<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   ///<summary>Awaits until <paramref name="condition"/> returns true for the shared object, holding no thread while it waits, then executes <paramref name="update"/><br/>
   /// within the update lock held while evaluating <paramref name="condition"/> and returns its result. Throws <see cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="timeout"/> expires.<br/>
   /// The async counterpart of <see cref="UpdateWhen{TResult}"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.</summary>
   Task<TResult> UpdateWhenAsync<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   ///<summary>Awaits until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires, holding no thread while it waits.<br/>
   /// If <paramref name="condition"/> became true, executes <paramref name="update"/> within the update lock held while evaluating it and returns true; else returns false without executing it.<br/>
   /// The async counterpart of <see cref="TryUpdateWhen(Func{TShared, bool}, Action{TShared}, CancellationToken, Nullable{WaitTimeout})"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.</summary>
   Task<bool> TryUpdateWhenAsync(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   ///<summary>Awaits until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires, holding no thread while it waits.<br/>
   /// Returns false if the wait times out, else true.<br/>
   /// The async counterpart of <see cref="TryAwait"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.</summary>
   Task<bool> TryAwaitAsync(Func<TShared, bool> condition, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   //async default implementations
   ///<summary>Awaits until <paramref name="condition"/> returns true for the shared object, holding no thread while it waits, then executes <paramref name="update"/><br/>
   /// within the update lock held while evaluating <paramref name="condition"/>. Throws <see cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="timeout"/> expires.<br/>
   /// The async counterpart of <see cref="UpdateWhen(Func{TShared, bool}, Action{TShared}, CancellationToken, Nullable{WaitTimeout})"/>.</summary>
   Task UpdateWhenAsync(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      UpdateWhenAsync(condition, update.ToFunc(), cancellationToken, timeout);

   ///<summary>Awaits until <paramref name="condition"/> returns true for the shared object, holding no thread while it waits. Throws <see cref="Exceptions.AwaitingConditionTimeoutException"/> if <paramref name="timeout"/> expires.<br/>
   /// The async counterpart of <see cref="Await"/>.</summary>
   Task AwaitAsync(Func<TShared, bool> condition, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
      ReadWhenAsync(condition, _ => unit, cancellationToken, timeout);
#pragma warning restore CA1068
}
