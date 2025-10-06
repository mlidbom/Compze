using Newtonsoft.Json;
#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it
#pragma warning disable CA2327 //Todo about this resides elsewhere search for CA2326 to find it

namespace Compze.Serialization;

static class JsonSettings
{
   internal static readonly JsonSerializerSettings JsonSerializerSettings =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
      };

   public static readonly JsonSerializerSettings SqlEventStoreSerializerSettings =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         ContractResolver = IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql.Instance
      };

}