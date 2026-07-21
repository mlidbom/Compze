using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Internals.SystemCE.ThreadingCE.Async;

///<summary>Provides <see cref="IAsyncLockCE"/>-serialized access to a shared object of type <typeparamref name="TShared"/> that is<br/>
/// produced asynchronously. The async counterpart of <see cref="IShared{TShared}"/>: where that serializes synchronous work on an<br/>
/// <see cref="ICriticalSection"/>, this serializes work on an <see cref="IAsyncLockCE"/>, so several operations awaited together run<br/>
/// one-at-a-time on the shared object instead of colliding on it.</summary>
///<remarks>The lock is re-entrant per async flow — <see cref="IAsyncLockCE"/> tracks entrance per flow — so a nested<br/>
/// <see cref="LockedAsync{TResult}"/> on the same flow does not deadlock, while independent flows serialize. The shared object is<br/>
/// awaited once and then reused across every call, so <see cref="IAsyncShared.New{TShared}"/> takes the still-resolving<br/>
/// <see cref="Task{TShared}"/> that produces it. The synchronous <see cref="Locked{TResult}"/> and asynchronous<br/>
/// <see cref="LockedAsync{TResult}"/> share the one lock, so sync and async operations on the shared object serialize against each<br/>
/// other too.</remarks>
public interface IAsyncShared<out TShared> : IDisposable
{
   ///<summary>Awaits the shared object, acquires the lock, passes the object to <paramref name="func"/>, awaits and returns its result, then releases the lock.</summary>
   Task<TResult> LockedAsync<TResult>(Func<TShared, Task<TResult>> func);

   ///<summary>Acquires the lock, passes the already-resolved shared object to <paramref name="func"/>, returns its result, then releases the lock.</summary>
   TResult Locked<TResult>(Func<TShared, TResult> func);

   ///<summary>Acquires the lock, passes the already-resolved shared object to <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked(Action<TShared> action) => Locked(action.ToFunc());
}

///<summary>Factory for <see cref="IAsyncShared{TShared}"/> instances.</summary>
public interface IAsyncShared
{
   ///<summary>Returns a new <see cref="IAsyncShared{TShared}"/> for the object produced by <paramref name="shared"/>, protected by a<br/>
   /// new <see cref="IAsyncLockCE"/> with the supplied <paramref name="timeout"/> — or the default timeout when none is given.</summary>
   public static IAsyncShared<TShared> New<TShared>(Task<TShared> shared, LockTimeout? timeout = null) =>
      new AsyncShared<TShared>(shared, timeout);

   internal class AsyncShared<TShared>(Task<TShared> shared, LockTimeout? timeout) : IAsyncShared<TShared>
   {
      readonly Task<TShared> _shared = shared;
      readonly IAsyncLockCE _serializedAccess = timeout is null ? IAsyncLockCE.WithDefaultTimeout() : IAsyncLockCE.New(timeout.Value);

      public async Task<TResult> LockedAsync<TResult>(Func<TShared, Task<TResult>> func)
      {
         var shared = await _shared.caf();
         return await _serializedAccess.LockedAsync(() => func(shared)).caf();
      }

      public TResult Locked<TResult>(Func<TShared, TResult> func) =>
         _serializedAccess.Locked(() => func(_shared.GetAwaiter().GetResult()));

      public void Dispose() => _serializedAccess.Dispose();
   }
}
