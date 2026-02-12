using Compze.Serialization.Newtonsoft.Private;
using Compze.Serialization.Newtonsoft.Private.TeventStore;
using Newtonsoft.Json;

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
         ContractResolver = new CompositeContractResolver(new IncludeMembersWithPrivateSetters())
      };

   internal static readonly JsonSerializerSettings DocumentDb = Default;

   internal static readonly JsonSerializerSettings Tessaging = Default;

   internal static readonly JsonSerializerSettings SharedObjects = Default;

   public static readonly JsonSerializerSettings TeventStore =
      new(Default)
      {
         ContractResolver = new CompositeContractResolver(new IncludeMembersWithPrivateSetters(),
                                                          new IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql())
      };
}
