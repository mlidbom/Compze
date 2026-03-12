namespace Compze.Threading.Interprocess;

/// <summary>
/// An <see cref="IAwaitableCriticalSection"/> backed by a system <see cref="Mutex"/> for cross-process synchronization.
/// Uses an <see cref="InterprocessChangeCounter"/> to avoid evaluating the user condition on every poll interval.
/// The condition is only evaluated when the counter changes (i.e., an update lock was released).
/// </summary>
public partial interface ISignalingAwaitableMutex : IAwaitableMutex
{
   ///<summary>Returns an <see cref="ISignalingAwaitableMutex"/> that synchronizes across all processes and user login sessions on the machine.</summary>
   public static ISignalingAwaitableMutex Global(string name, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutex = null) =>
      new SignalingAwaitableMutexCE(name, global: true, lockTimeout, waitTimeout, onAbandonedMutex);

   ///<summary>Returns an <see cref="ISignalingAwaitableMutex"/> that synchronizes across all processes within a single user login session on the machine.</summary>
   public static ISignalingAwaitableMutex Local(string name, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutex = null) =>
      new SignalingAwaitableMutexCE(name, global: false, lockTimeout, waitTimeout, onAbandonedMutex);
}
