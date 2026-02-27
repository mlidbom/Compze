using Compze.Functional;
using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial interface IMonitor
{
   unit Locked(Action action, TimeSpan? timeout = null) => Locked(action.AsFunc(), timeout);

   TReturn Locked<TReturn>(Func<TReturn> func, TimeSpan? timeout = null)
   {
      using(TakeLock(timeout)) return func();
   }
}
