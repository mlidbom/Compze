using Compze.SystemCE;

namespace Compze.Threading;

public partial interface ICriticalSection
{
   ///<summary>Acquires the lock, executes <paramref name="action"/>, then releases the lock.</summary>
   Unit Locked(Action action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);

   ///<summary>Acquires the lock, executes <paramref name="func"/> and returns its result, then releases the lock.</summary>
   TReturn Locked<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeLock(timeout)) return func();
   }
}
