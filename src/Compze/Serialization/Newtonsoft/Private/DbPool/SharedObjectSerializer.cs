using Compze.Core.Serialization.Internal.DbPool;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft.Private.DbPool;

class NewtonsoftSharedObjectSerializer<TShared> : ISharedObjectSerializer<TShared>
   where TShared : class
{
   static readonly JsonSerializerSettings JsonSettings = JsonSettings = new JsonSerializerSettings
                                                                        {
                                                                           ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                                                           ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
                                                                        };

   public string Serialize(TShared instance) => JsonConvert.SerializeObject(instance, Formatting.Indented, JsonSettings);
   public TShared Deserialize(string serialized) => JsonConvert.DeserializeObject<TShared>(serialized, JsonSettings).NotNull();
}
