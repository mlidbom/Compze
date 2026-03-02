using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Serialization.Newtonsoft.Private;

interface IJsonPropertyModifier
{
   // ReSharper disable once UnusedParameter.Global
   void ModifyProperty(JsonProperty property, MemberInfo memberInfo, MemberSerialization memberSerialization);
}
