using Compze.Contracts;
using MemoryPack;

namespace Compze.DbPool.MachineWideState;

class MemoryPackDbPoolStateSerializer : ISharedObjectSerializer<DbPoolState>
{
   internal static readonly MemoryPackDbPoolStateSerializer Instance = new();
   MemoryPackDbPoolStateSerializer(){}
   public string Serialize(DbPoolState instance) => Convert.ToBase64String(MemoryPackSerializer.Serialize(instance));

   public DbPoolState Deserialize(string json) =>
      MemoryPackSerializer.Deserialize<DbPoolState>(Convert.FromBase64String(json))._assert().NotNull();
}
