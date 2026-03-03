using Compze.Underscore;
using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IMonitor
{
   unit Locked(Action action, LockTimeout? timeout = null) => Locked(action.ToFunc(), timeout);

   TReturn Locked<TReturn>(Func<TReturn> func, LockTimeout? timeout = null)
   {
      using(TakeLock(timeout)) return func();
   }
}
