using System.Text.Json;
using System.Text.Json.Serialization;
using Compze.Abstractions;

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>Serializes a <see cref="TaggregateId"/> as the GUID it wraps, as a consumer's JSON options would.</summary>
sealed class TaggregateIdAsAGuid : JsonConverter<TaggregateId>
{
   public override TaggregateId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetGuid());
   public override void Write(Utf8JsonWriter writer, TaggregateId value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
