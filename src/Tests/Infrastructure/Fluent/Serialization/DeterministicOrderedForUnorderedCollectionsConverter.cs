using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Fluent.Serialization;

/// <summary>
/// Ensures dictionaries and sets serialize in a deterministic order for reliable comparison.
/// </summary>
class DeterministicOrderedForUnorderedCollectionsConverter : JsonConverter
{
   public override bool CanConvert(Type objectType) =>
      objectType.Implements(typeof(IDictionary<,>)) ||
      objectType.Implements(typeof(ISet<>));

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      if (value == null)
      {
         writer.WriteNull();
         return;
      }

      var objectType = value.GetType();

      // Handle dictionaries (IDictionary<TKey, TValue>)
      if (objectType.Implements(typeof(IDictionary<,>)))
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

      // Handle sets (ISet<T>)
      if (objectType.Implements(typeof(ISet<>)))
      {
         var enumerable = (IEnumerable)value;
         var orderedItems = enumerable.Cast<object?>().OrderBy(item => item?.ToString() ?? string.Empty).ToList();

         writer.WriteStartArray();
         foreach (var item in orderedItems)
         {
            serializer.Serialize(writer, item);
         }
         writer.WriteEndArray();
         return;
      }

      // Should never reach here given CanConvert check
      throw new InvalidOperationException($"Unexpected type: {objectType}");
   }

   public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
   {
      // We only need this for comparison, not deserialization
      throw new NotImplementedException("Deserialization is not needed for comparison");
   }
}
