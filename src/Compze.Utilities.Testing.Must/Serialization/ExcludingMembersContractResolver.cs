using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compze.Contracts;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Utilities.Testing.Must.Serialization;

internal class ExcludingMembersContractResolver : DefaultContractResolver
{
   readonly MemberFilteringContractResolver _baseResolver;
   readonly IReadOnlySet<MemberInfo> _excludedMembers;

   public ExcludingMembersContractResolver(MemberFilteringContractResolver baseResolver, IReadOnlySet<MemberInfo> excludedMembers)
   {
      _baseResolver = baseResolver;
      _excludedMembers = excludedMembers;
   }

   protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
   {
      var properties = _baseResolver.CreatePropertiesInternal(type, memberSerialization);
      return properties.Where(p => !ShouldExclude(type, p.PropertyName)).ToList();
   }

   bool ShouldExclude(Type objectType, string? propertyName)
   {
      if(string.IsNullOrEmpty(propertyName))
         return false;

      foreach(var excludedMember in _excludedMembers)
      {
         if(propertyName == excludedMember.Name && excludedMember.DeclaringType._assert().NotNull().IsAssignableFrom(objectType))
            return true;
      }

      return false;
   }
}
