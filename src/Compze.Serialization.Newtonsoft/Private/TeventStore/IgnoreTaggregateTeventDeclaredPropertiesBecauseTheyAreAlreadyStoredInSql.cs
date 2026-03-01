using System.Reflection;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft.Private.TeventStore;

internal class IgnoreTaggregateTeventDeclaredPropertiesBecauseTheyAreAlreadyStoredInSql : IJsonPropertyModifier
{
   public void ModifyProperty(JsonProperty property, MemberInfo memberInfo, MemberSerialization memberSerialization)
   {
      if(property.DeclaringType == typeof(TaggregateTevent))
      {
         property.Ignored = true;
      }
   }
}
