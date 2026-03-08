using Compze.Threading.ResourceAccess;

namespace Compze.Threading.Interprocess;

///<summary>
/// An <see cref="ILock"/> implementation backed by a system <see cref="Mutex"/> for cross-process synchronization.
/// Unlike in-process locks, a mutex owns a system resource and must be disposed.
/// </summary>
public partial interface IMutex : ILock, IDisposable
{
   ///<summary>Returns an <see cref="IMutex"/> that synchronizes across all processes and user login sessions on the machine.</summary>
   public static IMutex GlobalNamed(string name, LockTimeout? lockTimeout = null, Action? onAbandonedMutex = null)
   {
      ValidateName(name);
      return new MutexCE($@"Global\{name}", lockTimeout, onAbandonedMutex);
   }

   ///<summary>Returns an <see cref="IMutex"/> that synchronizes across all processes within a single user login session on the machine.</summary>
   public static IMutex LocalNamed(string name, LockTimeout? lockTimeout = null, Action? onAbandonedMutex = null)
   {
      ValidateName(name);
      return new MutexCE($@"Local\{name}", lockTimeout, onAbandonedMutex);
   }

   static void ValidateName(string name)
   {
      if(name.Contains('\\', StringComparison.Ordinal)) throw new ArgumentException("Name must not contain backslashes", nameof(name));
   }
}
