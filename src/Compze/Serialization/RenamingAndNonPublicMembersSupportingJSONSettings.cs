using Compze.Serialization.Newtonsoft.TeventStore;
using Newtonsoft.Json;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it
#pragma warning disable CA2327 //Todo about this resides elsewhere search for CA2326 to find it

namespace Compze.Serialization.Newtonsoft;

static class RenamingAndNonPublicMembersSupportingJSONSettings
{
   internal static readonly JsonSerializerSettings Default =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
      };

   internal static JsonSerializerSettings DocumentDb => Default;

   public static readonly JsonSerializerSettings TeventStore =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         ContractResolver = IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSqlResolver.Instance
      };

}