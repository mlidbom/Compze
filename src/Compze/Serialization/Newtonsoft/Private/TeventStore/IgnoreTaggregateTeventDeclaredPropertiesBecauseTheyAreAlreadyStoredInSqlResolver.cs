using System.Reflection;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft.Private.TeventStore;

class IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSqlResolver : IncludeMembersWithPrivateSettersResolver
{
   public new static readonly IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSqlResolver Instance = new();
   IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSqlResolver() {}

   protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
   {
      var property = base.CreateProperty(member, memberSerialization);

      if(property.DeclaringType == typeof(TaggregateTevent))
      {
         property.Ignored = true;
      }

      return property;
   }
}
