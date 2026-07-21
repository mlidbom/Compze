using Compze.SystemCE;
using JetBrains.Annotations;

namespace Compze.Threading.ResourceAccess;

///<summary>Provides lock-protected access to a shared object of type <typeparamref name="TShared"/>.</summary>
public interface IShared<out TShared>
{
   ///<summary>The underlying critical section used to protect access.</summary>
   ICriticalSection CriticalSection { get; }

#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   ///<summary>Acquires the lock, passes the shared object to <paramref name="func"/>, returns the result, then releases the lock.</summary>
   TResult Locked<TResult>([InstantHandle]Func<TShared, TResult> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null);

   ///<summary>Acquires the lock, passes the shared object to <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked([InstantHandle]Action<TShared> action, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Locked(action.ToFunc(), cancellationToken, timeout);
#pragma warning restore CA1068
}
