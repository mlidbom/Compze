using Compze.Contracts;
using MemoryPack;

namespace Compze.InterprocessObject.MemoryPack.Private;

///<summary>An <see cref="IInterprocessObjectSerializer{T}"/> that uses MemoryPack for fast binary serialization.</summary>
class MemoryPackInterprocessObjectSerializer<T> : IInterprocessObjectSerializer<T> where T : class
{
   ///<summary>Shared singleton instance. Safe to reuse — MemoryPack serialization is stateless.</summary>
   public static readonly MemoryPackInterprocessObjectSerializer<T> Instance = new();

   public byte[] Serialize(T instance) => MemoryPackSerializer.Serialize(instance);

   public T Deserialize(byte[] data) => MemoryPackSerializer.Deserialize<T>(data)._assert().NotNull();
}
