using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft.Private;

public class IncludeMembersWithPrivateSetters : IJsonPropertyModifier
{
   public void ModifyProperty(JsonProperty property, MemberInfo memberInfo, MemberSerialization memberSerialization)
   {
      if(!property.Writable)
      {
         var propertyInfo = memberInfo as PropertyInfo;
         if(propertyInfo != null)
         {
            var hasPrivateSetter = propertyInfo.GetSetMethod(true) != null;
            property.Writable = hasPrivateSetter;
         }
      }
   }
}
