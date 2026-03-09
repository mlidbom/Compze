namespace Compze.Threading.Interprocess;

/// <summary>
/// An <see cref="IAwaitableLock"/> backed by a system <see cref="Mutex"/> for cross-process synchronization.
/// Uses an <see cref="InterprocessChangeCounter"/> to avoid evaluating the user condition on every poll interval.
/// The condition is only evaluated when the counter changes (i.e., an update lock was released).
/// </summary>
public partial interface ISignalingAwaitableMutex : IMutex, IAwaitableLock
{
   public static ISignalingAwaitableMutex GlobalNamed(string name, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutex = null) =>
      new SignalingAwaitableMutexCE(name, global: true, lockTimeout, waitTimeout, onAbandonedMutex);

   public static ISignalingAwaitableMutex LocalNamed(string name, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null, Action? onAbandonedMutex = null) =>
      new SignalingAwaitableMutexCE(name, global: false, lockTimeout, waitTimeout, onAbandonedMutex);
}
