using System.Reflection;
using Compze.Teventive.Taggregates.Tevents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft._private.TeventStore;

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
