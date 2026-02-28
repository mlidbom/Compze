using System;
using Compze.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using MemoryPack;

namespace Compze.Utilities.Testing.DbPool;

public class MemoryPackDbPoolStateSerializer : ISharedObjectSerializer<DbPoolState>
{
   public static readonly MemoryPackDbPoolStateSerializer Instance = new();
   MemoryPackDbPoolStateSerializer(){}
   public string Serialize(DbPoolState instance) => Convert.ToBase64String(MemoryPackSerializer.Serialize(instance));

   public DbPoolState Deserialize(string json) =>
      MemoryPackSerializer.Deserialize<DbPoolState>(Convert.FromBase64String(json))._assert().NotNull();
}
