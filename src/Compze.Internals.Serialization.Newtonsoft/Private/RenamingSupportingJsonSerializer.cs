using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Newtonsoft.Json;

namespace Compze.Internals.Serialization.Newtonsoft.Private;

class RenamingSupportingJsonSerializer : IJsonSerializer
{
   readonly JsonSerializerSettings _jsonSettings;
   readonly RenamingDecorator _renamingDecorator;

   protected internal RenamingSupportingJsonSerializer(JsonSerializerSettings jsonSettings, ITypeMap typeMap)
   {
      _jsonSettings = jsonSettings;
      _renamingDecorator = new RenamingDecorator(typeMap);
   }

   public string Serialize(object instance)
   {
      var json = JsonConvert.SerializeObject(instance, Formatting.Indented, _jsonSettings);
      json = _renamingDecorator.ReplaceTypeNames(json);
      return json;
   }

   public object Deserialize(Type type, string json)
   {
      json = _renamingDecorator.RestoreTypeNames(json);
      return JsonConvert.DeserializeObject(json, type, _jsonSettings)!;
   }
}
