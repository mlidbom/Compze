﻿using System.Reflection;
using Compze.Persistence.EventStore;
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

      if(property.DeclaringType == typeof(AggregateEvent))
      {
         property.Ignored = true;
      }

      return property;
   }
}
