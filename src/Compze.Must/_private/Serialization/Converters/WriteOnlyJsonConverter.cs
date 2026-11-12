using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>A <see cref="JsonConverter"/> that only writes: Must serializes values to show and compare them, and never reads JSON back.</summary>
abstract class WriteOnlyJsonConverter : JsonConverter
{
   public override bool CanRead => false;

   public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
      throw new NotSupportedException("Must only writes JSON; it never reads any.");
}
