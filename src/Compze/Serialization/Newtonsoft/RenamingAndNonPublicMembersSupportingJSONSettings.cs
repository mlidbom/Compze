using Compze.Serialization.Newtonsoft.Private;
using Compze.Serialization.Newtonsoft.Private.PrimitiveWrappers;
using Compze.Serialization.Newtonsoft.Private.TeventStore;
using Newtonsoft.Json;
using System.Collections.Generic;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it
#pragma warning disable CA2327 //Todo about this resides elsewhere search for CA2326 to find it

namespace Compze.Serialization.Newtonsoft;

static class RenamingAndNonPublicMembersSupportingJsonSettings
{
   internal static readonly JsonSerializerSettings Default =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         Converters = new List<JsonConverter> { new EntityIdConverter() },
         ContractResolver = IncludeMembersWithPrivateSettersResolver.Instance
      };

   internal static JsonSerializerSettings DocumentDb => Default;

   internal static JsonSerializerSettings Tessaging => Default;

   public static readonly JsonSerializerSettings TeventStore =
      new()
      {
         TypeNameHandling = Default.TypeNameHandling,
         ConstructorHandling = Default.ConstructorHandling,
         Converters = Default.Converters,
         ContractResolver = IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSqlResolver.Instance,
      };

}