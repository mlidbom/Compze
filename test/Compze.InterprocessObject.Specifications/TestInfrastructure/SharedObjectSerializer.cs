using Compze.Contracts;
using MemoryPack;

namespace Compze.InterprocessObject.Specifications;

class SharedObjectSerializer : IInterprocessObjectSerializer<SharedObject>
{
   static SharedObjectSerializer()
   {
      // Force JIT + formatter registration so the cost in profiling isn't attributed to the first real call.
      var warmup = MemoryPackSerializer.Serialize(new SharedObject());
      MemoryPackSerializer.Deserialize<SharedObject>(warmup);
   }

   public byte[] Serialize(SharedObject instance) => MemoryPackSerializer.Serialize(instance);

   public SharedObject Deserialize(byte[] data) => MemoryPackSerializer.Deserialize<SharedObject>(data)._assert().NotNull();
}
