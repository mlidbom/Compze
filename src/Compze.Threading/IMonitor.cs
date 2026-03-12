namespace Compze.Threading;

///<summary>An in-process mutual-exclusion lock backed by a monitor. Equivalent to <see cref="ICriticalSection"/> but uses monitor semantics internally.</summary>
public interface IMonitor : ICriticalSection
{
   ///<summary>Returns a new <see cref="IMonitor"/> using the supplied <paramref name="timeout"/>. Uses <see cref="LockTimeout.Default"/> if <paramref name="timeout"/> is null.</summary>
   public static IMonitor New(LockTimeout? timeout = null) => IAwaitableMonitor.NewIMonitor(timeout);
}
