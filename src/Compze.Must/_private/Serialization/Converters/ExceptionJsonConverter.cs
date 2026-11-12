using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>Writes an exception as its type and message, instead of serializing its members.</summary>
/// <remarks>
/// Serializing an exception's members overflowed the stack, through the circular references between them, and took the
/// test runner down with it (commit 542baa4a1, 2025-11-02). Its remaining members, the stack trace and the method that
/// threw, are the runtime's internals and say nothing about the value under assertion.
/// </remarks>
class ExceptionJsonConverter : WriteOnlyJsonConverter
{
   public override bool CanConvert(Type objectType) => typeof(Exception).IsAssignableFrom(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      var exception = (Exception)value!;
      writer.WriteStartObject();

      writer.WritePropertyName("$type");
      writer.WriteValue(exception.GetType().FullName);

      writer.WritePropertyName("Message");
      writer.WriteValue(exception.Message);

      writer.WritePropertyName("_SerializationNote");
      writer.WriteValue("Full exception serialization skipped to prevent stack overflow from circular references");

      writer.WriteEndObject();
   }
}
