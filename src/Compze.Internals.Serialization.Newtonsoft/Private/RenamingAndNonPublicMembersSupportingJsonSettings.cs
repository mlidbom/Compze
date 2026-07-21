using Compze.Internals.Serialization.Newtonsoft.Private;
using Compze.Internals.Serialization.Newtonsoft.Private.TeventStore;
using Newtonsoft.Json;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it
#pragma warning disable CA2327 //Todo about this resides elsewhere search for CA2326 to find it

namespace Compze.Internals.Serialization.Newtonsoft.Private;

static class RenamingAndNonPublicMembersSupportingJsonSettings
{
   static readonly JsonSerializerSettings Default =
      new()
      {
         TypeNameHandling = TypeNameHandling.Auto,
         ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
         ContractResolver = new CompositeContractResolver(new IncludeMembersWithPrivateSetters())
      };

   public static readonly JsonSerializerSettings DocumentDb = Default;

   public static readonly JsonSerializerSettings Tessaging = Default;

   public static readonly JsonSerializerSettings Typermedia = Default;

   public static readonly JsonSerializerSettings TeventStore =
      new(Default)
      {
         ContractResolver = new CompositeContractResolver(new IncludeMembersWithPrivateSetters(),
                                                          new IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql())
      };
}
