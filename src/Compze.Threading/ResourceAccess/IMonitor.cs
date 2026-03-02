using System;

namespace Compze.Threading.ResourceAccess;

public partial interface IMonitor
{
   public static IMonitor New(LockTimeout? timeout = null) => IAwaitableMonitor.NewIMonitor(timeout);

   IDisposable TakeLock(LockTimeout? timeout = null);
}
