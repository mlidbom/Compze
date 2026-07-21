using Newtonsoft.Json;

namespace Compze.Must.Private.Serialization;

/// <summary>
/// Custom converter that prevents serialization of Exception objects to avoid stack overflow
/// caused by circular references in Exception properties (InnerException, Data, etc.)
/// </summary>
class ExceptionJsonConverter : JsonConverter
{
   public override bool CanConvert(Type objectType) => typeof(Exception).IsAssignableFrom(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      if(value == null)
      {
         writer.WriteNull();
         return;
      }

      var exception = (Exception)value;
      writer.WriteStartObject();

      writer.WritePropertyName("$type");
      writer.WriteValue(exception.GetType().FullName);

      writer.WritePropertyName("Message");
      writer.WriteValue(exception.Message);

      writer.WritePropertyName("_SerializationNote");
      writer.WriteValue("Full exception serialization skipped to prevent stack overflow from circular references");

      writer.WriteEndObject();
   }

   public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
   {
      // We don't need to deserialize in tests
      throw new NotImplementedException("Exception deserialization is not supported");
   }
}
