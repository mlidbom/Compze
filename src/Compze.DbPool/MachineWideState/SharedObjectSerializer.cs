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

   public string Serialize(DbPoolState instance)
   {
      var inArray = MemoryPackSerializer.Serialize(instance);
      return Convert.ToBase64String(inArray);
   }

   public DbPoolState Deserialize(string json)
   {
      var fromBase64String = Convert.FromBase64String(json);
      var dbPoolState = MemoryPackSerializer.Deserialize<DbPoolState>(fromBase64String);
      return dbPoolState._assert().NotNull();
   }
}
