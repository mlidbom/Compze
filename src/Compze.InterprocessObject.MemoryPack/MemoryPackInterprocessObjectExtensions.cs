using Compze.Threading;

namespace Compze.InterprocessObject.MemoryPack;

///<summary>Convenience factory methods for creating <see cref="IInterprocessObject{T}"/> instances using MemoryPack serialization.</summary>
public static class MemoryPackInterprocessObject
{
   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a global cross-process mutex, using MemoryPack for serialization.
   ///<para>This is the recommended way to create interprocess objects — MemoryPack provides fast, compact binary serialization ideal for memory-mapped storage.</para>
   ///</summary>
   public static IInterprocessObject<T> New<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => IInterprocessObject.Create(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, maxCapacityInBytes, lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, synchronized with a session-local cross-process mutex, using MemoryPack for serialization.</summary>
   public static IInterprocessObject<T> NewLocal<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => IInterprocessObject.CreateLocal(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, maxCapacityInBytes, lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a regular file, synchronized with a global cross-process mutex, using MemoryPack for serialization.</summary>
   public static IInterprocessObject<T> NewFileBacked<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => IInterprocessObject.CreateFileBacked(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, lockTimeout, waitTimeout);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a regular file, synchronized with a session-local cross-process mutex, using MemoryPack for serialization.</summary>
   public static IInterprocessObject<T> NewFileBackedLocal<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) where T : class
      => IInterprocessObject.CreateFileBackedLocal(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, lockTimeout, waitTimeout);
}
