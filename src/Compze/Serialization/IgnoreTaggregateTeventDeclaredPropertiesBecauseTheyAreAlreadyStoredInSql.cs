using System.Reflection;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft;

class IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IncludeMembersWithPrivateSettersResolver
{
   public new static readonly IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql Instance = new();
   IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql() {}

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
