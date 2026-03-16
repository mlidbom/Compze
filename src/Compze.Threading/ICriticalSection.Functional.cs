using Compze.SystemCE;

namespace Compze.Threading;

public partial interface ICriticalSection
{
   ///<summary>Acquires the lock, executes <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked(Action action, CancellationToken cancellationToken = default, LockTimeout? timeout = null) => Locked(action.ToFunc(), cancellationToken, timeout);

   ///<summary>Acquires the lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn Locked<TReturn>(Func<TReturn> func, CancellationToken cancellationToken = default, LockTimeout? timeout = null)
   {
      using(TakeLock(cancellationToken, timeout)) return func();
   }
}
