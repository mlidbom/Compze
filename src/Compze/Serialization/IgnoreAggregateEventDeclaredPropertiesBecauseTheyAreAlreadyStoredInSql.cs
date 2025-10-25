using System.Reflection;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization;

class IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IncludeMembersWithPrivateSettersResolver
{
   public new static readonly IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql Instance = new();
   IgnoreAggregateEventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql() {}

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
