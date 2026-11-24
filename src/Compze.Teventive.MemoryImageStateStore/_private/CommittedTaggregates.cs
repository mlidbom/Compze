using Compze.Teventive.MemoryImageStateStore._internal;
using System.Collections.Concurrent;
using Compze.Abstractions;
using Compze.Teventive._internal;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Threading;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>The taggregates a memory image state store holds as committed: read only instances that any thread reads without a lock, the<br/>
/// number of the commit that stored each, where the storage numbers its commits, and the indexes that file them. Whatever changes<br/>
/// them installs under one lock: a load, a unit of work's commit, and what the store finds that other memory images committed. A<br/>
/// state is installed only if it was committed after the one held, so none installs an older state over a newer one.</summary>
sealed class CommittedTaggregates
{
   readonly IReadOnlyList<ITaggregateIndexMaintenance> _indexes;
   readonly IMonitor _installingMonitor = IMonitor.New();

   //Replaced whole by a load, so that a reader sees either what was committed before it or what it loaded.
   volatile ConcurrentDictionary<TaggregateId, ITaggregate> _instances = new();

   //Read and written under _installingMonitor. Kept for a deleted taggregate too, so that a state read before its deletion can't bring it back.
   Dictionary<TaggregateId, CommitNumber> _commitNumbers = new();

   internal CommittedTaggregates(IReadOnlyList<ITaggregateIndexMaintenance> indexes) => _indexes = indexes;

   internal ITaggregate? InstanceOf(TaggregateId id) => _instances.GetValueOrDefault(id);

   ///<summary>The committed instances by id, as they are now. Enumerated, it doesn't copy, nor take every lock of the dictionary, as its<br/>
   /// <c>Values</c> would; a load that replaces them meanwhile doesn't change what an enumeration begun before it gives.</summary>
   internal IReadOnlyDictionary<TaggregateId, ITaggregate> Instances => _instances;

   ///<summary>The number of the commit that stored the taggregate <paramref name="id"/> as held, or deleted it; null where the storage<br/>
   /// numbers no commits, or the taggregate was never stored.</summary>
   internal CommitNumber? CommitNumberOf(TaggregateId id) => _installingMonitor.Locked(() => _commitNumbers.TryGetValue(id, out var number) ? number : (CommitNumber?)null);

   ///<summary>Replaces everything with <paramref name="loaded"/>, what a load restored, each with the commit that stored it, if the<br/>
   /// storage numbers its commits. The indexes file exactly them.</summary>
   internal void ReplaceWith(IReadOnlyList<(ITaggregate Taggregate, CommitNumber? CommittedAs)> loaded)
   {
      var instances = new ConcurrentDictionary<TaggregateId, ITaggregate>(loaded.Select(it => KeyValuePair.Create(it.Taggregate.Id, it.Taggregate)));
      foreach(var taggregate in instances.Values) MakeReadOnly(taggregate);
      _installingMonitor.Locked(() =>
      {
         foreach(var index in _indexes) index.FileExactly(instances.Values);
         _commitNumbers = loaded.Where(it => it.CommittedAs != null).ToDictionary(it => it.Taggregate.Id, it => it.CommittedAs!.Value);
         _instances = instances;
      });
   }

   ///<summary>Installs a unit of work's commit: each of <paramref name="changed"/> in its new state, or removed for a deleted one, as<br/>
   /// the commit <paramref name="committedAs"/> stored them, and <paramref name="indexChanges"/>, which its prepare computed.</summary>
   ///<remarks>Unconditionally: the unit of work holds each taggregate's lock, here and in the storage, so its commit is the newest of each.</remarks>
   internal void InstallACommit(IReadOnlyList<(TaggregateId Id, ITaggregate? NewState)> changed, CommitNumber? committedAs, IReadOnlyList<IPreparedIndexChange> indexChanges) =>
      _installingMonitor.Locked(() =>
      {
         foreach(var (id, newState) in changed) Install(id, newState, committedAs);
         foreach(var indexChange in indexChanges) indexChange.Apply();
      });

   ///<summary>Installs <paramref name="found"/>, taggregates the storage holds as other memory images committed them, each with its<br/>
   /// state restored, or null for one deleted, wherever it was committed after the commit held. Re-files the indexes, which runs<br/>
   /// the consumer's functions from state to keys, before installing anything: so if one fails, nothing is installed.</summary>
   ///<returns>How what readers see changed.</returns>
   internal ChangesFoundInStorage InstallFound(IReadOnlyList<(StoredTaggregate Stored, ITaggregate? Restored)> found) =>
      _installingMonitor.Locked(() =>
      {
         var newer = found.Where(it => !_commitNumbers.TryGetValue(it.Stored.Id, out var held) || it.Stored.CommittedAs > held).ToList();
         var indexChanges = _indexes.Select(index => index.Prepare([..newer.Select(it => (it.Stored.Id, NewState: it.Restored))])).ToList();
         var changes = new ChangesFoundInStorage(added: [..newer.Where(it => it.Restored != null && InstanceOf(it.Stored.Id) == null).Select(it => it.Stored.Id)],
                                                 changed: [..newer.Where(it => it.Restored != null && InstanceOf(it.Stored.Id) != null).Select(it => it.Stored.Id)],
                                                 removed: [..newer.Where(it => it.Restored == null && InstanceOf(it.Stored.Id) != null).Select(it => it.Stored.Id)]);
         foreach(var it in newer) Install(it.Stored.Id, it.Restored, it.Stored.CommittedAs);
         foreach(var indexChange in indexChanges) indexChange.Apply();
         return changes;
      });

   ///<summary>Records that the commit <paramref name="committedAs"/> stored every committed taggregate anew, in the state held.</summary>
   internal void RecordEveryTaggregateAsCommittedBy(CommitNumber committedAs) =>
      _installingMonitor.Locked(() =>
      {
         foreach(var id in _instances.Keys) _commitNumbers[id] = committedAs;
      });

   void Install(TaggregateId id, ITaggregate? newState, CommitNumber? committedAs)
   {
      if(newState == null)
      {
         _instances.TryRemove(id, out _);
      } else
      {
         MakeReadOnly(newState);
         _instances[id] = newState;
      }

      if(committedAs is {} number) _commitNumbers[id] = number;
   }

   static void MakeReadOnly(ITaggregate taggregate) => ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
}
