using System.Reflection;
using Compze.Teventive.Taggregates.Tevents.Public;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Internals.Serialization.Newtonsoft.Private.TeventStore;

class IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IJsonPropertyModifier
{
   public void ModifyProperty(JsonProperty property, MemberInfo memberInfo, MemberSerialization memberSerialization)
   {
      if(property.DeclaringType == typeof(TaggregateTevent))
      {
         property.Ignored = true;
      }
   }
}
