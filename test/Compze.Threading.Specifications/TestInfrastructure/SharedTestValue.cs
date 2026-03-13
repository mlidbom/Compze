using Compze.Contracts;
using Compze.InterprocessObject;
using MemoryPack;

namespace Compze.Threading.Specifications.TestInfrastructure;

[MemoryPackable]
partial class SharedTestValue
{
   public int Value { get; set; }
   public List<int> Items { get; set; } = [];
}

class SharedTestValueSerializer : IInterprocessObjectSerializer<SharedTestValue>
{
   static SharedTestValueSerializer()
   {
      var warmup = MemoryPackSerializer.Serialize(new SharedTestValue());
      MemoryPackSerializer.Deserialize<SharedTestValue>(warmup);
   }

   public byte[] Serialize(SharedTestValue instance) => MemoryPackSerializer.Serialize(instance);
   public SharedTestValue Deserialize(byte[] data) => MemoryPackSerializer.Deserialize<SharedTestValue>(data)._assert().NotNull();
}
