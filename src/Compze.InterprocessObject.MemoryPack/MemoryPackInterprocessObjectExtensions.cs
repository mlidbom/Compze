namespace Compze.InterprocessObject.MemoryPack;

///<summary>Convenience factory methods for creating <see cref="IInterprocessObject{T}"/> instances using MemoryPack serialization.</summary>
public static class MemoryPackInterprocessObjectExtensions
{
   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a memory-mapped file, using MemoryPack for serialization.
   ///<para>This is the recommended way to create interprocess objects — MemoryPack provides fast, compact binary serialization ideal for memory-mapped storage.</para>
   ///</summary>
   public static IInterprocessObject<T> CreateMemoryPack<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction, int maxCapacityInBytes) where T : class
      => IInterprocessObject.Create(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction, maxCapacityInBytes);

   ///<summary>Creates a new <see cref="IInterprocessObject{T}"/> backed by a regular file, using MemoryPack for serialization.</summary>
   public static IInterprocessObject<T> CreateFileBackedMemoryPack<T>(string name, Func<T> createDefault, CorruptionAction corruptionAction) where T : class
      => IInterprocessObject.CreateFileBacked(name, MemoryPackInterprocessObjectSerializer<T>.Instance, createDefault, corruptionAction);
}
