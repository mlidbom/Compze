using System.Text.Json;
using Compze.Teventive.Taggregates._internal;

namespace Compze.Teventive.MemoryImageStateStore._private.Storage;

///<summary>The JSON that Compze's storages, the files and the tables, hold of each taggregate: exactly what the consumer's<br/>
/// <see cref="JsonSerializerOptions"/> make of its snapshot, as the snapshot type of its class.</summary>
sealed class SnapshotJson
{
   readonly JsonSerializerOptions _options;
   readonly IReadOnlyDictionary<Type, SnapshotableTaggregateClass> _classes;

   ///<summary>The JSON of the snapshots of <paramref name="classes"/>, as <paramref name="options"/> write and read it.</summary>
   internal SnapshotJson(JsonSerializerOptions options, IEnumerable<SnapshotableTaggregateClass> classes)
   {
      _options = options;
      _classes = classes.ToDictionary(it => it.Class);
   }

   ///<summary>What the storage holds of <paramref name="snapshot"/>, a snapshot of a <paramref name="taggregateClass"/>.</summary>
   internal string Of(Type taggregateClass, object snapshot) => JsonSerializer.Serialize(snapshot, _classes[taggregateClass].SnapshotType, _options);

   ///<summary>The snapshot of a <paramref name="taggregateClass"/> that <paramref name="json"/> holds, or null for JSON that holds null.</summary>
   internal object? SnapshotIn(string json, SnapshotableTaggregateClass taggregateClass) => JsonSerializer.Deserialize(json, taggregateClass.SnapshotType, _options);

   ///<summary>Whether <paramref name="loaded"/> holds the state <paramref name="heldSnapshot"/> holds, as the storage writes state: if the text it<br/>
   /// was read from is what the storage would write for <paramref name="heldSnapshot"/>, or else if <paramref name="loaded"/>'s snapshot<br/>
   /// serializes to that text too, the storage holding the same state written otherwise, such as indented differently.</summary>
   internal bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot)
   {
      var held = Of(loaded.TaggregateClass, heldSnapshot);
      return loaded.TextReadFrom == held || Of(loaded.TaggregateClass, loaded.Snapshot) == held;
   }
}
