using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Utilities.Testing.Must.Serialization;

/// <summary>
/// Ensures dictionaries and sets serialize in a deterministic order for reliable comparison.
/// </summary>
class DeterministicOrderedForUnorderedCollectionsConverter : JsonConverter
{
   public override bool CanConvert(Type objectType) =>
      objectType.ImplementsGenericInterface(typeof(IDictionary<,>)) ||
      objectType.ImplementsGenericInterface(typeof(ISet<>));

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      if(value == null)
      {
         writer.WriteNull();
         return;
      }

      var objectType = value.GetType();

      // Handle dictionaries (IDictionary<TKey, TValue>)
      if(objectType.ImplementsGenericInterface(typeof(IDictionary<,>)))
      {
         var dictionary = (IDictionary)value;
         var orderedKeys = dictionary.Keys.Cast<object?>().OrderBy(k => k?.ToString() ?? string.Empty).ToList();

         writer.WriteStartObject();

         // Preserve type information
         if(serializer.TypeNameHandling != TypeNameHandling.None)
         {
            writer.WritePropertyName("$type");
            writer.WriteValue($"{objectType.FullName}, {objectType.Assembly.GetName().Name}");
         }

         foreach(var key in orderedKeys)
         {
            if(key != null)
            {
               writer.WritePropertyName(key.ToString() ?? string.Empty);
               serializer.Serialize(writer, dictionary[key]);
            }
         }

         writer.WriteEndObject();
         return;
      }

      // Handle sets (ISet<T>)
      if(objectType.ImplementsGenericInterface(typeof(ISet<>)))
      {
         var enumerable = (IEnumerable)value;
         var orderedItems = enumerable.Cast<object?>().OrderBy(item => item?.ToString() ?? string.Empty).ToList();

         writer.WriteStartObject();

         // Preserve type information
         if(serializer.TypeNameHandling != TypeNameHandling.None)
         {
            writer.WritePropertyName("$type");
            writer.WriteValue($"{objectType.FullName}, {objectType.Assembly.GetName().Name}");
            writer.WritePropertyName("$values");
         }

         writer.WriteStartArray();
         foreach(var item in orderedItems)
         {
            serializer.Serialize(writer, item);
         }

         writer.WriteEndArray();

         if(serializer.TypeNameHandling != TypeNameHandling.None)
         {
            writer.WriteEndObject();
         }

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
