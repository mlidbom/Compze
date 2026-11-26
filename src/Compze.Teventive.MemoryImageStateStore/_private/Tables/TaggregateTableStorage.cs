using Compze.Teventive.MemoryImageStateStore._internal;
using System.Diagnostics;
using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Teventive.MemoryImageStateStore._private.Storage;

namespace Compze.Teventive.MemoryImageStateStore._private.Tables;

///<summary>Compze's storage of a memory image state store in tables of the domain database, which several memory images share: a row for<br/>
/// each taggregate, holding its class's declared name, its snapshot as the consumer's JSON options write it, and the number of the<br/>
/// commit that last wrote it. See the tables' design, the-memory-image-state-store-in-tables.md in Compze's dev_docs.</summary>
///<remarks>Composed with <see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>. The SQL is the domain database's<br/>
/// <see cref="IMemoryImageStateStoreSqlLayer"/>, which a package of its own registers, such as Compze.Teventive.MemoryImageStateStore.Sqlite's.</remarks>
sealed class TaggregateTableStorage : ITaggregateStorage
{
   readonly IMemoryImageStateStoreSqlLayer _sqlLayer;
   readonly SnapshotJson _snapshotJson;
   readonly TaggregateClassNames _classNames;

   internal TaggregateTableStorage(IMemoryImageStateStoreSqlLayer sqlLayer, SnapshotJson snapshotJson, TaggregateClassNames classNames)
   {
      _sqlLayer = sqlLayer;
      _snapshotJson = snapshotJson;
      _classNames = classNames;
      CommitSignal = new CommitSignal(sqlLayer.DatabaseIdentity);
   }

   ///<summary>The database's commit signal, raised once each commit of this memory image to the tables has committed.</summary>
   internal CommitSignal CommitSignal { get; }

   ///<summary>Every live row of the declared classes, read outside any transaction, with the last commit number, read before them.</summary>
   public LoadedTaggregates LoadAll()
   {
      var read = OutsideAnyTransaction(() => _sqlLayer.ReadAll(_classNames.Names));
      return new LoadedTaggregates([..read.Rows.Where(it => it.Snapshot != null).Select(SnapshotIn)], read.HoldingEveryCommitThrough);
   }

   ///<summary>Never asked: a reload compares taggregates kept in tables by their commit numbers.</summary>
   public bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot) => throw new UnreachableException("A reload compares the taggregates of a memory image state store kept in tables by their commit numbers.");

   public StoredTaggregate? LockForUpdate(TaggregateId id, CommitNumber? heldAs) =>
      _sqlLayer.LockAndReadUnlessHeldAs(id, heldAs) is {} row && _classNames.Declares(row.ClassName) ? Stored(row) : null;

   public TaggregatesCommittedAfter CommittedAfter(CommitNumber lastSeen)
   {
      var read = OutsideAnyTransaction(() => _sqlLayer.ReadCommittedAfter(lastSeen, _classNames.Names));
      return new TaggregatesCommittedAfter([..read.Rows.Select(Stored)], read.HoldingEveryCommitThrough);
   }

   ///<summary>Serializes each of <paramref name="toSave"/>, which can fail here, and has the write take part in the unit of work's<br/>
   /// transaction, to take its commit's number and write its rows at the very end of the transaction's prepare.</summary>
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete) =>
      PreparedTableWrite.TakingPartInTheAmbientTransaction(_sqlLayer, [..toSave.Select(RowToSave)], toDelete, onceCommitted: CommitSignal.Raise);

   ///<summary>In a transaction of its own: takes the commit counter first, so that no other memory image commits meanwhile, hands<br/>
   /// <paramref name="everythingOnceCaughtUpWith"/> what the other memory images committed after <paramref name="lastSeen"/>, then<br/>
   /// writes every taggregate it returns, as one commit.</summary>
   public CommitNumber? WriteAll(CommitNumber lastSeen, Func<IReadOnlyList<StoredTaggregate>, IReadOnlyList<TaggregateSnapshot>> everythingOnceCaughtUpWith)
   {
      var committedAs = TransactionScopeCe.Execute(() =>
      {
         var takenNumber = _sqlLayer.TakeTheNextCommitNumber();
         var committedMeanwhile = _sqlLayer.ReadCommittedAfter(lastSeen, _classNames.Names);
         var everything = everythingOnceCaughtUpWith([..committedMeanwhile.Rows.Select(Stored)]);
         _sqlLayer.Write(takenNumber, [..everything.Select(RowToSave)], deleted: []);
         return takenNumber;
      }, TransactionScopeOption.RequiresNew);
      CommitSignal.Raise();
      return committedAs;
   }

   TaggregateRowToSave RowToSave(TaggregateSnapshot snapshot) =>
      new(snapshot.Id, _classNames.NameOf(snapshot.TaggregateClass), _snapshotJson.Of(snapshot.TaggregateClass, snapshot.Snapshot));

   StoredTaggregate Stored(TaggregateRow row) =>
      row.Snapshot == null ? StoredTaggregate.Deleted(row.Id, row.CommittedAs) : StoredTaggregate.WithState(SnapshotIn(row), row.CommittedAs);

   ///<summary>The snapshot <paramref name="row"/> holds, and the taggregate restored from it: whatever fails is rethrown naming the row.</summary>
   TaggregateSnapshot SnapshotIn(TaggregateRow row)
   {
      var taggregateClass = _classNames.ClassNamed(row.ClassName);
      try
      {
         var snapshot = _snapshotJson.SnapshotIn(row.Snapshot!, taggregateClass) ?? throw new JsonException("The row holds null.");
         return new TaggregateSnapshot(taggregateClass.Class, row.Id, snapshot)
                {
                   RestoredTaggregate = taggregateClass.Restore(row.Id, snapshot),
                   CommittedAs = row.CommittedAs
                };
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The memory image state store can't load the row of taggregate {row.Id}, a {taggregateClass.Class.Name} by its class name \"{row.ClassName}\", written by {row.CommittedAs}: {exception.Message}", exception);
      }
   }

   public void Dispose() => CommitSignal.Dispose();

   static TResult OutsideAnyTransaction<TResult>(Func<TResult> read) => TransactionScopeCe.Execute(read, TransactionScopeOption.Suppress);
}
