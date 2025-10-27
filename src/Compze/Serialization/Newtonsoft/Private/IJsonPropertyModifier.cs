using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft.Private;

interface IJsonPropertyModifier
{
   void ModifyProperty(JsonProperty property, MemberInfo memberInfo, MemberSerialization memberSerialization);
}
