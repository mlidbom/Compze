// ReSharper disable ConvertToPrimaryConstructor

using Compze.SystemCE;

namespace Compze.Threading.ResourceAccess;

///<summary>Contains the internal base implementation for <see cref="IAwaitableShared{TShared}"/>.</summary>
public interface IAwaitableShared
{
   internal class AwaitableShared<TShared> : IAwaitableShared<TShared>
   {
      readonly TShared _shared;

      public AwaitableShared(TShared shared, IAwaitableCriticalSection criticalSection)
      {
         _shared = shared;
         CriticalSection = criticalSection;
      }

      public IAwaitableCriticalSection CriticalSection { get; }

      public TResult Read<TResult>(Func<TShared, TResult> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null) =>
         CriticalSection.Read(() => read(_shared), cancellationToken, timeout);

      public TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
         CriticalSection.ReadWhen(() => condition(_shared), () => read(_shared), cancellationToken, waitTimeout: timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null) =>
         CriticalSection.Update(() => update(_shared), cancellationToken, timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
         CriticalSection.UpdateWhen(() => condition(_shared), () => update(_shared), cancellationToken, waitTimeout: timeout);

      public bool TryUpdateWhen(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) =>
         CriticalSection.TryUpdateWhen(() => condition(_shared), () => update(_shared), cancellationToken, waitTimeout: timeout);
   }
}

///<summary>Provides lock-protected access to a shared object of type <typeparamref name="TShared"/> with read/update semantics and condition-based waiting.</summary>
public interface IAwaitableShared<out TShared>
{
   ///<summary>The underlying critical section used to protect access.</summary>
   IAwaitableCriticalSection CriticalSection { get; }

#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   //core
   ///<summary>Acquires a read lock, passes the shared object to <paramref name="read"/>, returns the result, then releases the lock.</summary>
   TResult Read<TResult>(Func<TShared, TResult> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null);
   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires a read lock, executes <paramref name="read"/>, and returns its result.</summary>
   TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);
   ///<summary>Acquires an update lock, passes the shared object to <paramref name="update"/>, returns the result, then releases the lock.</summary>
   TResult Update<TResult>(Func<TShared, TResult> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null);
   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock, executes <paramref name="update"/>, and returns its result.</summary>
   TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);

   //Default implementations
   ///<summary>Acquires a read lock, passes the shared object to <paramref name="read"/>, then releases the lock.</summary>
   Unit Read(Action<TShared> read, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Read(read.ToFunc(), cancellationToken, timeout);

   ///<summary>Acquires an update lock, passes the shared object to <paramref name="update"/>, then releases the lock.</summary>
   Unit Update(Action<TShared> update, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Update(update.ToFunc(), cancellationToken, timeout);
   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock and executes <paramref name="update"/>.</summary>
   Unit UpdateWhen(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) => UpdateWhen(condition, update.ToFunc(), cancellationToken, timeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object or <paramref name="timeout"/> expires.</summary>
   Unit Await(Func<TShared, bool> condition, CancellationToken cancellationToken = default, WaitTimeout? timeout = null) => ReadWhen(condition, _ => unit, cancellationToken, timeout);

   ///<summary>Blocks until <paramref name="condition"/> returns true for the shared object, then acquires an update lock and executes <paramref name="update"/>. Returns false if the wait times out, else true.</summary>
   bool TryUpdateWhen(Func<TShared, bool> condition, Action<TShared> update, CancellationToken cancellationToken = default, WaitTimeout? timeout = null);
#pragma warning restore CA1068
}
