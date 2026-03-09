namespace Compze.Threading;

public interface IMonitor : ILock
{
   public static IMonitor New(LockTimeout? timeout = null) => IAwaitableMonitor.NewIMonitor(timeout);
}
