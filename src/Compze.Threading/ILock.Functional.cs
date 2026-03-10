using Compze.SystemCE;

namespace Compze.Threading;

public partial interface ILock
{
   Unit Locked(Action action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);

   TReturn Locked<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeLock(timeout)) return func();
   }
}
