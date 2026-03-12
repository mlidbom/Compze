namespace Compze.Threading.Interprocess;

///<summary>An <see cref="IAwaitableLock"/> backed by a system <see cref="Mutex"/> for cross-process synchronization. Must be disposed to release the underlying OS handle.</summary>
public interface IAwaitableMutex : IAwaitableLock, IDisposable
{
   ///<summary>True if the mutex synchronizes across all user login sessions on the machine; false if scoped to the current session.</summary>
   bool IsGlobal { get; }
   ///<summary>The system name of the mutex, including the <c>Global\</c> or <c>Local\</c> prefix.</summary>
   string Name { get; }
}
