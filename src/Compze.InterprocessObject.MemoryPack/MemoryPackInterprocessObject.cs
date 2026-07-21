using Compze.Threading;
using Compze.InterprocessObject.MemoryPack._private;

namespace Compze.InterprocessObject.MemoryPack;

///<summary>Convenience factory methods for creating <see cref="IInterprocessObject{T}"/> instances using MemoryPack serialization.</summary>
public static class MemoryPackInterprocessObject
{
   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a global cross-process mutex, using MemoryPack for serialization.
   ///<para>This is the recommended way to create interprocess objects — MemoryPack provides fast, compact binary serialization ideal for memory-mapped storage.</para>
   ///</summary>
   public static IInterprocessObject<T> NewGlobal<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, int maxBytes, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => IInterprocessObject.NewGlobal(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, maxBytes, directory, lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a session-local cross-process mutex, using MemoryPack for serialization.</summary>
   public static IInterprocessObject<T> NewLocal<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, int maxBytes, DirectoryInfo directory, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => IInterprocessObject.NewLocal(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, maxBytes, directory, lockTimeout, waitTimeout);
}
