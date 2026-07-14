using System.Text.Json;
using System.Text.Json.Serialization;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Internals.Transport;

///<summary>The fixed wire format of the endpoint-discovery conversation. Deliberately not pluggable: discovery is the handshake<br/>
/// that must work between any two Compze endpoints before anything is known about how they are composed — so the framework owns<br/>
/// the format outright, and no serializer choice a composition makes can break two endpoints' ability to find each other.</summary>
///<remarks>Communication styles contribute their own discovery queries (Tessaging's <see cref="EndpointInformationQuery"/>,<br/>
/// Typermedia's endpoint-information query), so the payload set is open. A contributed payload must be a plain wire shape this<br/>
/// fixed format can handle: one deserialization constructor (<c>[JsonConstructor]</c> when there are several) whose parameters<br/>
/// match the get-only properties, over primitives, collections, <see cref="EndpointId"/>, and <see cref="EndpointAddress"/>.</remarks>
public static class EndpointDiscoverySerializer
{
   static readonly JsonSerializerOptions WireFormat = new()
   {
      Converters = { new EndpointIdJsonConverter(), new EndpointAddressJsonConverter() }
   };

   public static string SerializeQuery(IMessage query) => JsonSerializer.Serialize(query, query.GetType(), WireFormat);
   public static IMessage DeserializeQuery(Type queryType, string json) => (IMessage)JsonSerializer.Deserialize(json, queryType, WireFormat)!;

   public static string SerializeResult(object result) => JsonSerializer.Serialize(result, result.GetType(), WireFormat);
   public static TResult DeserializeResult<TResult>(string json) => JsonSerializer.Deserialize<TResult>(json, WireFormat)!;

   class EndpointIdJsonConverter : JsonConverter<EndpointId>
   {
      public override EndpointId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetGuid());
      public override void Write(Utf8JsonWriter writer, EndpointId value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
   }

   class EndpointAddressJsonConverter : JsonConverter<EndpointAddress>
   {
      public override EndpointAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(new Uri(reader.GetString()!));
      public override void Write(Utf8JsonWriter writer, EndpointAddress value, JsonSerializerOptions options) => writer.WriteStringValue(value.Uri.AbsoluteUri);
   }
}
