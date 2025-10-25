using System.Reflection;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization;

class IgnoreAggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IncludeMembersWithPrivateSettersResolver
{
   public new static readonly IgnoreAggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql Instance = new();
   IgnoreAggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql() {}

   protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
   {
      var property = base.CreateProperty(member, memberSerialization);

      if(property.DeclaringType == typeof(AggregateTevent))
      {
         property.Ignored = true;
      }

      return property;
   }
}
