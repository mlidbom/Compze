using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Threading;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private.Tables;

///<summary>Compze's storage of a memory image state store in tables of the domain database: a row for each taggregate, holding its class's<br/>
/// declared name and its snapshot as the consumer's JSON options write it. See the tables' design,<br/>
/// the-memory-image-state-store-in-tables.md in Compze's dev_docs.</summary>
///<remarks>Composed with <see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>. The SQL is the domain database's<br/>
/// <see cref="IMemoryImageStateStoreSqlLayer"/>, which a package of its own registers, such as Compze.Teventive.MemoryImageStateStore.Sqlite's.</remarks>
///<remarks>One memory image at a time uses a database's tables: the first load takes the domain database's process lock<br/>
/// <see cref="TheTablesLockName"/>, and the storage holds it until disposed. Should the database session holding it die, as when the database<br/>
/// becomes unreachable, another memory image may take the tables, so this one commits nothing more.</remarks>
sealed class TaggregateTableStorage : ITaggregateStorage
{
   ///<summary>The name of the domain database's process lock that the memory image using its tables holds: one for the database, whichever<br/>
   /// taggregate classes the store keeps.</summary>
   const string TheTablesLockName = "Compze.Teventive.MemoryImageStateStore";

   const string WhereItIsKept = "the domain database's tables";

   readonly IMemoryImageStateStoreSqlLayer _sqlLayer;
   readonly SnapshotJson _snapshotJson;
   readonly TaggregateClassNames _classNames;
   readonly WaitTimeout _waitForTheTables;

   //Taken by the first load, under the store's loading monitor.
   IDisposable? _theTablesLock;

   //Set on the lock's keepalive task when the session holding it dies.
   volatile Exception? _theTablesLockWasLostBecause;

   ///<summary>The storage in the tables <paramref name="sqlLayer"/> reads and writes, whose first load waits at most <paramref name="waitForTheTables"/><br/>
   /// for another memory image to release them.</summary>
   internal TaggregateTableStorage(IMemoryImageStateStoreSqlLayer sqlLayer, SnapshotJson snapshotJson, TaggregateClassNames classNames, WaitTimeout waitForTheTables)
   {
      _sqlLayer = sqlLayer;
      _snapshotJson = snapshotJson;
      _classNames = classNames;
      _waitForTheTables = waitForTheTables;
   }

   ///<summary>Takes the tables' lock, at the first load, then reads every row of the declared classes, outside any transaction. Each snapshot<br/>
   /// keeps the text it was read from, for a reload to compare with what the store would write for the taggregate it held.</summary>
   public IReadOnlyList<TaggregateSnapshot> LoadAll()
   {
      _theTablesLock ??= _sqlLayer.TryTakeProcessLock(TheTablesLockName, StorageHeldByAnotherMemoryImage.PurposeOfTheLock(WhereItIsKept), _waitForTheTables, RecordThatTheTablesLockWasLost)
                      ?? throw StorageHeldByAnotherMemoryImage.Refusal(WhereItIsKept, _sqlLayer.ProcessLockHolder(TheTablesLockName), _waitForTheTables);
      //bug: the read outside the transaction takes a connection from the domain database's pool, the pool units of work hold their
      //   transactions' connections from. The first load runs at the store's first use, which may be in a unit of work that holds its
      //   transaction's connection, and a lock other units of work wait for holding theirs, such as a tevent-store taggregate's lock:
      //   with every connection held, the load gets none, and all wait until the pool times out. Those waiting at the loading monitor
      //   may hold theirs too. Found 2026-11-27 while fixing the tevent stores' second connections, the same deadlock: see
      //   Given_a_tevent_store_holding_a_user. No specification reproduces it yet.
      return [..TransactionScopeCe.Execute(() => _sqlLayer.ReadAll(_classNames.Names), TransactionScopeOption.Suppress).Select(SnapshotIn)];
   }

   void RecordThatTheTablesLockWasLost(Exception sessionDeath)
   {
      _theTablesLockWasLostBecause = sessionDeath;
      this.Log().Error(sessionDeath, $"The memory image state store kept in {WhereItIsKept} lost its lock on them: the database session holding it died. Another memory image may take them now, so this one commits nothing more.");
   }

   ///<summary>Asserts that this memory image holds the tables, as it does from its first load on, unless the database session holding their lock died.</summary>
   void AssertTheTablesAreStillHeld()
   {
      State.Assert(_theTablesLock != null, () => "The memory image state store writes its tables before it has loaded them, and so before it holds them.");
      if(_theTablesLockWasLostBecause is {} sessionDeath)
         throw new InvalidOperationException($"The memory image state store kept in {WhereItIsKept} lost its lock on them when the database session holding it died, so another memory image may have taken them since. This memory image commits nothing more: dispose its container, and start the store anew once the database is reachable.", sessionDeath);
   }

   ///<summary>Whether <paramref name="loaded"/> holds the state <paramref name="heldSnapshot"/> holds, as the rows hold state: see<br/>
   /// <see cref="SnapshotJson.HoldTheSameState"/>.</summary>
   public bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot) => _snapshotJson.HoldTheSameState(loaded, heldSnapshot);

   ///<summary>Serializes each of <paramref name="toSave"/>, then writes the rows in the unit of work's transaction, which is preparing: they<br/>
   /// commit, or roll back, with it, as the database's commit is the transaction's decision. So nothing is left to do when the unit of work<br/>
   /// completes.</summary>
   ///<remarks>Safe without a lock on each taggregate in the database: the unit of work holds each taggregate's lock in memory, and the memory<br/>
   /// image holding the tables' lock is the only one that writes them, so no other transaction writes the same rows meanwhile.</remarks>
   public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete)
   {
      if(toSave.Count == 0 && toDelete.Count == 0) return WrittenInTheTransaction.Instance;
      AssertTheTablesAreStillHeld();
      var rows = toSave.Select(RowToSave).ToList();
      Transaction.Current._assert().NotNull();
      _sqlLayer.Write(rows, toDelete);
      return WrittenInTheTransaction.Instance;
   }

   ///<summary>Writes every row anew, in a transaction of its own.</summary>
   public void WriteAll(IReadOnlyList<TaggregateSnapshot> everything)
   {
      AssertTheTablesAreStillHeld();
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

   ///<summary>Releases the tables' lock, for the next memory image to take.</summary>
   public void Dispose() => _theTablesLock?.Dispose();

   ///<summary>A write made in the unit of work's transaction already, which commits or rolls back with it: nothing is left to do.</summary>
   sealed class WrittenInTheTransaction : IPreparedWrite
   {
      internal static readonly WrittenInTheTransaction Instance = new();

      public void Commit() {}
      public void Discard() {}
   }
}
