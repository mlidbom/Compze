using System.Reflection;
using Compze.Utilities.SystemCE.LinqCE;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft.Private;

public class CompositeContractResolver(params IJsonPropertyModifier[] modifiers) : DefaultContractResolver
{
   readonly IJsonPropertyModifier[] _modifiers = modifiers;

   protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
   {
      JsonProperty property = base.CreateProperty(member, memberSerialization);
      _modifiers.ForEach(it => it.ModifyProperty(property, member, memberSerialization));
      return property;
   }
}
