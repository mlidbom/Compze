using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

/// <summary>
/// Ensures dictionaries and sets serialize in a deterministic order for reliable comparison.
/// </summary>
class OrderedCollectionConverter : JsonConverter
{
   public override bool CanConvert(Type objectType) =>
      objectType.IsGenericType &&
      (objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
       objectType.GetGenericTypeDefinition() == typeof(HashSet<>) ||
       objectType.GetGenericTypeDefinition() == typeof(ISet<>));

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      if (value == null)
      {
         writer.WriteNull();
         return;
      }

      var objectType = value.GetType();

      // Handle dictionaries
      if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
      {
         var dictionary = (IDictionary)value;
         var orderedKeys = dictionary.Keys.Cast<object?>().OrderBy(k => k?.ToString() ?? string.Empty).ToList();

         writer.WriteStartObject();
         foreach (var key in orderedKeys)
         {
            if (key != null)
            {
               writer.WritePropertyName(key.ToString() ?? string.Empty);
               serializer.Serialize(writer, dictionary[key]);
            }
         }
         writer.WriteEndObject();
         return;
      }

      // Handle sets
      if (objectType.IsGenericType && 
          (objectType.GetGenericTypeDefinition() == typeof(HashSet<>) ||
           objectType.GetGenericTypeDefinition() == typeof(ISet<>)))
      {
         var enumerable = (IEnumerable)value;
         var orderedItems = enumerable.Cast<object>().OrderBy(item => item?.ToString() ?? string.Empty).ToList();

         writer.WriteStartArray();
         foreach (var item in orderedItems)
         {
            serializer.Serialize(writer, item);
         }
         writer.WriteEndArray();
         return;
      }

      // Fallback to default serialization
      JToken.FromObject(value).WriteTo(writer);
   }

   public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
   {
      // We only need this for comparison, not deserialization
      throw new NotImplementedException("Deserialization is not needed for comparison");
   }
}
