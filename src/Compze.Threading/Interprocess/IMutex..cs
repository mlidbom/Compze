namespace Compze.Threading.Interprocess;

///<summary>
/// An <see cref="ICriticalSection"/> implementation backed by a system <see cref="Mutex"/> for cross-process synchronization.
/// Unlike in-process locks, a mutex owns a system resource and must be disposed.
/// </summary>
public partial interface IMutex : ICriticalSection, IDisposable
{
   ///<summary>Returns an <see cref="IMutex"/> that synchronizes across all processes and user login sessions on the machine.</summary>
   public static IMutex Global(string name, LockTimeout? lockTimeout = null, Action? onAbandonedMutex = null) =>
      new MutexCE(name, global:true, lockTimeout, onAbandonedMutex);

   ///<summary>Returns an <see cref="IMutex"/> that synchronizes across all processes within a single user login session on the machine.</summary>
   public static IMutex Local(string name, LockTimeout? lockTimeout = null, Action? onAbandonedMutex = null) =>
      new MutexCE(name, global:false, lockTimeout, onAbandonedMutex);

   ///<summary>True if the mutex synchronizes across all user login sessions on the machine; false if scoped to the current session.</summary>
   bool IsGlobal { get; }
   ///<summary>The system name of the mutex, including the <c>Global\</c> or <c>Local\</c> prefix.</summary>
   string Name { get; }

   ///<summary>Attempts to acquire the mutex within <paramref name="timeout"/>. Returns null if the timeout expires. Uses <see cref="ICriticalSectionInfo.LockTimeout"/> if <paramref name="timeout"/> is null.</summary>
   internal ILock? TryTakeLock(LockTimeout? timeout = null);

   ///<summary>Releases all nesting levels held by the current thread and returns the depth that was released. After this call, no thread holds the mutex. Used by <see cref="IAwaitableMutex"/> to implement condition-wait semantics analogous to <see cref="System.Threading.Monitor.Wait(object)"/>.</summary>
   internal int ReleaseAllNestingLevels();

   ///<summary>Reacquires the mutex <paramref name="depth"/> times, restoring the nesting level to the state before a prior <see cref="ReleaseAllNestingLevels"/> call.</summary>
   internal void ReacquireToNestingDepth(int depth, LockTimeout? timeout = null);
}
