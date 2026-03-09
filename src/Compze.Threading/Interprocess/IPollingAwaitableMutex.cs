namespace Compze.Threading.Interprocess;

///<summary>
/// An <see cref="IAwaitableLock"/> implementation backed by a system <see cref="Mutex"/> for cross-process synchronization.
/// Condition waiting is implemented via polling — the mutex is periodically acquired and the condition checked.
/// This is less efficient than in-process monitor-based waiting but enables cross-process condition synchronization.
/// </summary>
public partial interface IPollingAwaitableMutex : IAwaitableMutex
{
   ///<summary>Returns an <see cref="IPollingAwaitableMutex"/> that synchronizes across all processes and user login sessions on the machine.</summary>
   public static IPollingAwaitableMutex Global(string name, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutex = null) =>
      new PollingAwaitableMutexCE(name, global: true, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutex);

   ///<summary>Returns an <see cref="IPollingAwaitableMutex"/> that synchronizes across all processes within a single user login session on the machine.</summary>
   public static IPollingAwaitableMutex Local(string name, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, PollingInterval? pollingInterval = null, Action? onAbandonedMutex = null) =>
      new PollingAwaitableMutexCE(name, global: false, lockTimeout, waitTimeout, pollingInterval, onAbandonedMutex);

   PollingInterval PollingInterval { get; }
}
