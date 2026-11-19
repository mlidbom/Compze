using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Compze.Abstractions;

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>Serializes a <see cref="TaggregateId"/> as the GUID it wraps, as <see cref="TaggregateIdAsAGuid"/> does, and records each id it<br/>
/// writes, so that a specification sees whose snapshots the store serializes. Told to, it refuses to write one id, as a snapshot that<br/>
/// can't be serialized.</summary>
sealed class WatchedTaggregateIdConverter : JsonConverter<TaggregateId>
{
   readonly ConcurrentQueue<TaggregateId> _written = new();
   TaggregateId? _refused;

   ///<summary>Each id written since <see cref="ForgetWhatWasWritten"/>, once for each time.</summary>
   internal IReadOnlyCollection<TaggregateId> Written => [.._written];

   internal void ForgetWhatWasWritten() => _written.Clear();

   ///<summary>Has every later write of <paramref name="id"/> throw.</summary>
   internal void RefuseToWrite(TaggregateId id) => Volatile.Write(ref _refused, id);

   public override TaggregateId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetGuid());

   public override void Write(Utf8JsonWriter writer, TaggregateId value, JsonSerializerOptions options)
   {
      if(Volatile.Read(ref _refused) == value) throw new JsonException($"The specification refuses to have taggregate {value} written.");
      _written.Enqueue(value);
      writer.WriteStringValue(value.Value);
   }
}
