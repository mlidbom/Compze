using Compze.Threading.Interprocess._internal;

namespace Compze.Threading.Interprocess;

/// <summary>
/// An <see cref="IAwaitableCriticalSection"/> backed by a system <see cref="Mutex"/> for cross-process synchronization.
/// Uses an <see cref="InterprocessChangeCounter"/> to avoid evaluating the user condition on every poll interval.
/// The condition is only evaluated when the counter changes (i.e., an update lock was released).
/// The <see cref="ISignalPollingPolicy"/> decides how eagerly waiters poll for such changes, trading signal-detection latency against power draw.
/// Must be disposed to release the underlying OS handle.
/// </summary>
public partial interface IAwaitableMutex : IAwaitableCriticalSection, IDisposable
{
   ///<summary>Returns an <see cref="IAwaitableMutex"/> that synchronizes across all processes and user login sessions on the machine.</summary>
   public static IAwaitableMutex Global(string name, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, ISignalPollingPolicy? signalPollingPolicy = null, Action? onAbandonedMutex = null) =>
      new AwaitableMutex(name, global: true, directory, lockTimeout, waitTimeout, signalPollingPolicy, onAbandonedMutex);

   ///<summary>Returns an <see cref="IAwaitableMutex"/> that synchronizes across all processes within a single user login session on the machine.</summary>
   public static IAwaitableMutex Local(string name, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, ISignalPollingPolicy? signalPollingPolicy = null, Action? onAbandonedMutex = null) =>
      new AwaitableMutex(name, global: false, directory, lockTimeout, waitTimeout, signalPollingPolicy, onAbandonedMutex);

   ///<summary>True if the mutex synchronizes across all user login sessions on the machine; false if scoped to the current session.</summary>
   bool IsGlobal { get; }
   ///<summary>The system name of the mutex, including the <c>Global\</c> or <c>Local\</c> prefix.</summary>
   string Name { get; }
}
