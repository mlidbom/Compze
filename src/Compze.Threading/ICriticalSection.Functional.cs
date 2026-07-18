using Compze.SystemCE;
using JetBrains.Annotations;

namespace Compze.Threading;

public partial interface ICriticalSection
{
#pragma warning disable CA1068 // Passing cancellation token around is standard practice in modern .NET while the timeout overrides are very rarely used. We don't want to force the common case to use named parameters.
   ///<summary>Acquires the lock, executes <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked([InstantHandle]Action action, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Locked(action.ToFunc(), cancellationToken, timeout);

   ///<summary>Acquires the lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn Locked<TReturn>([InstantHandle]Func<TReturn> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null)
   {
      using(TakeLock(cancellationToken, timeout)) return func();
   }
#pragma warning restore CA1068
}
