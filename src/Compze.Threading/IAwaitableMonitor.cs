namespace Compze.Threading;

///<summary>An in-process <see cref="IAwaitableCriticalSection"/> backed by a monitor. Threads blocked in condition-wait methods are woken when an update lock is released.</summary>
public partial interface IAwaitableMonitor : IAwaitableCriticalSection
{
   ///<summary>Returns a new <see cref="IAwaitableMonitor"/> using default timeouts.</summary>
   public static IAwaitableMonitor WithDefaultTimeout() => new MonitorCE();
   ///<summary>Returns a new <see cref="IAwaitableMonitor"/> using the supplied timeouts. Uses <see cref="LockTimeout.Default"/> and <see cref="WaitTimeout.Default"/> for null parameters.</summary>
   public static IAwaitableMonitor New(LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) => new MonitorCE(lockTimeout, waitTimeout);

   ///<summary>Returns a new <see cref="IMonitor"/> using the supplied <paramref name="timeout"/>. Uses <see cref="LockTimeout.Default"/> if <paramref name="timeout"/> is null.</summary>
   internal static IMonitor NewIMonitor(LockTimeout? timeout = null) => new MonitorCE(timeout);
}
