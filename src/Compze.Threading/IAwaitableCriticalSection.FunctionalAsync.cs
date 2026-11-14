using Compze.Threading.Exceptions;

namespace Compze.Threading;

public partial interface IAwaitableCriticalSection
{
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   ///<summary>Awaits until <paramref name="condition"/> returns true, holding no thread while it waits, then - within the same read lock held while evaluating<br/>
   /// <paramref name="condition"/> - executes <paramref name="func"/> and returns its result. Throws <see cref="AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires.<br/>
   /// The async counterpart of <see cref="ReadWhen{TReturn}"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.<br/>
   /// <paramref name="func"/> must not change the state this critical section protects, and neither may <paramref name="condition"/>: releasing a read lock wakes no wait. <see cref="IAwaitableCriticalSection"/> tells why.</summary>
   async Task<TReturn> ReadWhenAsync<TReturn>(Func<bool> condition, Func<TReturn> func, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      TReturn result = default!;
      return await TryAwaitAsync(ReadOnceTheConditionHolds, cancellationToken, waitTimeout, lockTimeout).ConfigureAwait(false)
                ? result
                : throw new AwaitingConditionTimeoutException();

      //Evaluated under the read lock, so a read in the same evaluation that found the condition true sees the state that satisfied it.
      bool ReadOnceTheConditionHolds()
      {
         if(!condition()) return false;
         result = func();
         return true;
      }
   }

   ///<summary>Awaits until <paramref name="condition"/> returns true, holding no thread while it waits, then executes <paramref name="action"/> within the update<br/>
   /// lock held while evaluating <paramref name="condition"/>, notifying all waits that there are updates. Throws <see cref="AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires.<br/>
   /// The async counterpart of <see cref="UpdateWhen(Func{bool},Action,CancellationToken,Compze.Threading.WaitTimeout?,Compze.Threading.LockTimeout?)"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.</summary>
   async Task UpdateWhenAsync(Func<bool> condition, Action action, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      if(!await TryUpdateWhenAsync(condition, action, cancellationToken, waitTimeout, lockTimeout).ConfigureAwait(false))
         throw new AwaitingConditionTimeoutException();
   }

   ///<summary>Awaits until <paramref name="condition"/> returns true, holding no thread while it waits, then executes <paramref name="func"/> within the update<br/>
   /// lock held while evaluating <paramref name="condition"/>, notifying all waits that there are updates, and returns its result. Throws <see cref="AwaitingConditionTimeoutException"/> if <paramref name="waitTimeout"/> expires.<br/>
   /// The async counterpart of <see cref="UpdateWhen{TReturn}"/>: see <see cref="IAwaitableCriticalSection"/> for how async condition waits work.</summary>
   async Task<TReturn> UpdateWhenAsync<TReturn>(Func<bool> condition, Func<TReturn> func, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null)
   {
      TReturn result = default!;
      return await TryUpdateWhenAsync(condition, () => result = func(), cancellationToken, waitTimeout, lockTimeout).ConfigureAwait(false)
                ? result
                : throw new AwaitingConditionTimeoutException();
   }
#pragma warning restore CA1068
}
