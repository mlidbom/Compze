using System.Reflection;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization;

class IncludeMembersWithPrivateSettersResolver : DefaultContractResolver, IStaticInstancePropertySingleton<IncludeMembersWithPrivateSettersResolver>
{
   public static IncludeMembersWithPrivateSettersResolver Instance { get; } = new();
   protected IncludeMembersWithPrivateSettersResolver()
   {
   }

   protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
   {
      var prop = base.CreateProperty(member, memberSerialization);

      if(!prop.Writable)
      {
         var property = member as PropertyInfo;
         if(property != null)
         {
            var hasPrivateSetter = property.GetSetMethod(true) != null;
            prop.Writable = hasPrivateSetter;
         }
      }

      return prop;
   }
}