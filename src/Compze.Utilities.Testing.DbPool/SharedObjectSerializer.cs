using Compze.Contracts;
using Compze.Serialization.Newtonsoft;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.DbPool;

public class DbPoolStateSerializer : ISharedObjectSerializer<DbPoolState>
{
   public static readonly DbPoolStateSerializer Instance = new();
   DbPoolStateSerializer(){}
   public string Serialize(DbPoolState instance) => JsonConvert.SerializeObject(instance, Formatting.Indented, RenamingAndNonPublicMembersSupportingJsonSettings.SharedObjects);

   public DbPoolState Deserialize(string json) =>
      JsonConvert.DeserializeObject<DbPoolState>(json, RenamingAndNonPublicMembersSupportingJsonSettings.SharedObjects)._assert().NotNull();
}
