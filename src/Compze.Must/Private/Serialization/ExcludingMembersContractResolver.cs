using System.Reflection;
using Compze.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Must.Private.Serialization;

class ExcludingMembersContractResolver(MemberFilteringContractResolver baseResolver, IReadOnlySet<MemberInfo> excludedMembers) : DefaultContractResolver
{
   readonly MemberFilteringContractResolver _baseResolver = baseResolver;
   readonly IReadOnlySet<MemberInfo> _excludedMembers = excludedMembers;

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
