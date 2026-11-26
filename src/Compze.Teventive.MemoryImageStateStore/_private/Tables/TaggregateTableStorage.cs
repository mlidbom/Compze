using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Teventive.MemoryImageStateStore._private.Storage;

namespace Compze.Teventive.MemoryImageStateStore._private.Tables;

///<summary>Compze's storage of a memory image state store in tables of the domain database: a row for each taggregate, holding its class's<br/>
/// declared name and its snapshot as the consumer's JSON options write it. See the tables' design,<br/>
/// the-memory-image-state-store-in-tables.md in Compze's dev_docs.</summary>
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
   }

   ///<summary>Every row of the declared classes, read outside any transaction. Each snapshot keeps the text it was read from, for a reload to<br/>
   /// compare with what the store would write for the taggregate it held.</summary>
   public IReadOnlyList<TaggregateSnapshot> LoadAll() =>
      [..TransactionScopeCe.Execute(() => _sqlLayer.ReadAll(_classNames.Names), TransactionScopeOption.Suppress).Select(SnapshotIn)];

   ///<summary>Whether <paramref name="loaded"/> holds the state <paramref name="heldSnapshot"/> holds, as the rows hold state: see<br/>
   /// <see cref="SnapshotJson.HoldTheSameState"/>.</summary>
   public bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot) => _snapshotJson.HoldTheSameState(loaded, heldSnapshot);

   ///<summary>Serializes each of <paramref name="toSave"/>, then writes the rows in the unit of work's transaction, which is preparing: they<br/>
   /// commit, or roll back, with it, as the database's commit is the transaction's decision. So nothing is left to do when the unit of work<br/>
   /// completes.</summary>
   ///<remarks>Safe without a lock in the database: the unit of work holds each taggregate's lock in memory, and the memory image holding the<br/>
   /// tables' lock is the only one that writes them, so no other transaction writes the same rows meanwhile.</remarks>
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete)
   {
      if(toSave.Count == 0 && toDelete.Count == 0) return WrittenInTheTransaction.Instance;
      var rows = toSave.Select(RowToSave).ToList();
      Transaction.Current._assert().NotNull();
      _sqlLayer.Write(rows, toDelete);
      return WrittenInTheTransaction.Instance;
   }

   ///<summary>Writes every row anew, in a transaction of its own.</summary>
   public void WriteAll(IReadOnlyList<TaggregateSnapshot> everything)
   {
      var rows = everything.Select(RowToSave).ToList();
      TransactionScopeCe.Execute(() => _sqlLayer.Write(rows, deleted: []), TransactionScopeOption.RequiresNew);
   }

   TaggregateRowToSave RowToSave(TaggregateSnapshot snapshot) =>
      new(snapshot.Id, _classNames.NameOf(snapshot.TaggregateClass), _snapshotJson.Of(snapshot.TaggregateClass, snapshot.Snapshot));

   ///<summary>The snapshot <paramref name="row"/> holds, and the taggregate restored from it: whatever fails is rethrown naming the row.</summary>
   TaggregateSnapshot SnapshotIn(TaggregateRow row)
   {
      var taggregateClass = _classNames.ClassNamed(row.ClassName);
      try
      {
         var snapshot = _snapshotJson.SnapshotIn(row.Snapshot, taggregateClass) ?? throw new JsonException("The row holds null.");
         return new TaggregateSnapshot(taggregateClass.Class, row.Id, snapshot)
                {
                   RestoredTaggregate = taggregateClass.Restore(row.Id, snapshot),
                   TextReadFrom = row.Snapshot
                };
      }
      catch(Exception exception)
      {
         throw new InvalidOperationException($"The memory image state store can't load the row of taggregate {row.Id}, a {taggregateClass.Class.Name} by its class name \"{row.ClassName}\": {exception.Message}", exception);
      }
   }

   public void Dispose() {}

   ///<summary>A write made in the unit of work's transaction already, which commits or rolls back with it: nothing is left to do.</summary>
   sealed class WrittenInTheTransaction : IPreparedWrite
   {
      internal static readonly WrittenInTheTransaction Instance = new();

      public void Commit() {}
      public void Discard() {}
   }
}
