using System;
using Compze.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Threading.ResourceAccess;
using MemoryPack;

namespace Compze.Utilities.Testing.DbPool;

internal class MemoryPackDbPoolStateSerializer : ISharedObjectSerializer<DbPoolState>
{
   internal static readonly MemoryPackDbPoolStateSerializer Instance = new();
   MemoryPackDbPoolStateSerializer(){}
   public string Serialize(DbPoolState instance) => Convert.ToBase64String(MemoryPackSerializer.Serialize(instance));

   public DbPoolState Deserialize(string json) =>
      MemoryPackSerializer.Deserialize<DbPoolState>(Convert.FromBase64String(json))._assert().NotNull();
}
