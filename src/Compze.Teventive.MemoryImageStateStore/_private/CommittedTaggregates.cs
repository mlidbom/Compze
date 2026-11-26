using System.Collections.Concurrent;
using Compze.Abstractions;
using Compze.Teventive._internal;
using Compze.Threading;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>The taggregates a memory image state store holds as committed: read only instances that any thread reads without a lock, and the<br/>
/// indexes that file them. Whatever changes them installs under one lock: a load, and a unit of work's commit.</summary>
sealed class CommittedTaggregates
{
   readonly IReadOnlyList<ITaggregateIndexMaintenance> _indexes;
   readonly IMonitor _installingMonitor = IMonitor.New();

   //Replaced whole by a load, so that a reader sees either what was committed before it or what it loaded.
   volatile ConcurrentDictionary<TaggregateId, ITaggregate> _instances = new();

   internal CommittedTaggregates(IReadOnlyList<ITaggregateIndexMaintenance> indexes) => _indexes = indexes;

   internal ITaggregate? InstanceOf(TaggregateId id) => _instances.GetValueOrDefault(id);

   ///<summary>The committed instances by id, as they are now. Enumerated, it doesn't copy, nor take every lock of the dictionary, as its<br/>
   /// <c>Values</c> would; a load that replaces them meanwhile doesn't change what an enumeration begun before it gives.</summary>
   internal IReadOnlyDictionary<TaggregateId, ITaggregate> Instances => _instances;

   ///<summary>Replaces everything with <paramref name="loaded"/>, what a load restored. The indexes file exactly them.</summary>
   internal void ReplaceWith(IReadOnlyList<ITaggregate> loaded)
   {
      var instances = new ConcurrentDictionary<TaggregateId, ITaggregate>(loaded.Select(it => KeyValuePair.Create(it.Id, it)));
      foreach(var taggregate in instances.Values) MakeReadOnly(taggregate);
      _installingMonitor.Locked(() =>
      {
         foreach(var index in _indexes) index.FileExactly(instances.Values);
         _instances = instances;
      });
   }

   ///<summary>Installs a unit of work's commit: each of <paramref name="changed"/> in its new state, or removed for a deleted one, and<br/>
   /// <paramref name="indexChanges"/>, which its prepare computed.</summary>
   ///<remarks>Unconditionally: the unit of work holds each taggregate's lock, so its commit is the newest of each.</remarks>
   internal void InstallACommit(IReadOnlyList<(TaggregateId Id, ITaggregate? NewState)> changed, IReadOnlyList<IPreparedIndexChange> indexChanges) =>
      _installingMonitor.Locked(() =>
      {
         foreach(var (id, newState) in changed) Install(id, newState);
         foreach(var indexChange in indexChanges) indexChange.Apply();
      });

   void Install(TaggregateId id, ITaggregate? newState)
   {
      if(newState == null)
      {
         _instances.TryRemove(id, out _);
      } else
      {
         MakeReadOnly(newState);
         _instances[id] = newState;
      }
   }

   static void MakeReadOnly(ITaggregate taggregate) => ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
}
