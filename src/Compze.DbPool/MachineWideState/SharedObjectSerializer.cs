using Compze.Contracts;
using Compze.Threading.Interprocess.ResourceAccess;
using MemoryPack;

namespace Compze.DbPool.MachineWideState;

class MemoryPackDbPoolStateSerializer : ISharedObjectSerializer<DbPoolState>
{
   internal static readonly MemoryPackDbPoolStateSerializer Instance = new();

   static MemoryPackDbPoolStateSerializer()
   {
      // Force JIT + formatter registration so the cost in profiling isn't attributed to the first real call.
      var warmup = MemoryPackSerializer.Serialize(new DbPoolState());
      MemoryPackSerializer.Deserialize<DbPoolState>(warmup);
   }

   public byte[] Serialize(DbPoolState instance) => MemoryPackSerializer.Serialize(instance);

   public DbPoolState Deserialize(byte[] data) => MemoryPackSerializer.Deserialize<DbPoolState>(data)._assert().NotNull();
}
