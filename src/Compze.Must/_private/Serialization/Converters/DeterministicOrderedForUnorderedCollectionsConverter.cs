using System.Collections;
using Compze.Internals.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>
/// Ensures dictionaries and sets serialize in a deterministic order for reliable comparison.
/// </summary>
class DeterministicOrderedForUnorderedCollectionsConverter : WriteOnlyJsonConverter
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

      if(objectType.ImplementsGenericInterface(typeof(IDictionary<,>)))
      {
         WriteDictionary(writer, value, objectType, serializer);
      } else
      {
         WriteSet(writer, (IEnumerable)value, objectType, serializer);
      }
   }

   static void WriteDictionary(JsonWriter writer, object dictionary, Type objectType, JsonSerializer serializer)
   {
      writer.WriteStartObject();

      if(serializer.TypeNameHandling != TypeNameHandling.None)
      {
         writer.WritePropertyName("$type");
         writer.WriteValue($"{objectType.FullName}, {objectType.Assembly.GetName().Name}");
      }

      foreach(var (key, entryValue) in EntriesOf(dictionary).OrderBy(it => it.Key.ToString() ?? string.Empty))
      {
         writer.WritePropertyName(key.ToString() ?? string.Empty);
         serializer.Serialize(writer, entryValue);
      }

      writer.WriteEndObject();
   }

   static void WriteSet(JsonWriter writer, IEnumerable set, Type objectType, JsonSerializer serializer)
   {
      var orderedItems = set.Cast<object?>().OrderBy(item => item?.ToString() ?? string.Empty).ToList();

      if(serializer.TypeNameHandling != TypeNameHandling.None)
      {
         writer.WriteStartObject();
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
   }

   /// <summary>
   /// The entries of a dictionary, through the non-generic <see cref="IDictionary"/> when it implements it and through its
   /// <see cref="KeyValuePair{TKey,TValue}"/>s otherwise: many dictionaries implement only the generic interface,
   /// such as System.Text.Json's <c>JsonObject</c>.
   /// </summary>
   static IEnumerable<(object Key, object? Value)> EntriesOf(object dictionary) =>
      dictionary is IDictionary nonGenericDictionary
         ? nonGenericDictionary.Keys.Cast<object>().Select(key => (key, nonGenericDictionary[key]))
         : ((IEnumerable)dictionary).Cast<object>().Select(KeyAndValueOf);

   static (object Key, object? Value) KeyAndValueOf(object keyValuePair)
   {
      var pairType = keyValuePair.GetType();
      return (pairType.GetProperty(nameof(KeyValuePair<,>.Key))!.GetValue(keyValuePair)!,
              pairType.GetProperty(nameof(KeyValuePair<,>.Value))!.GetValue(keyValuePair));
   }
}
