using Compze.Teventive.MemoryImageStateStore._internal;
using System.Transactions;
using Compze.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private.Tables;

///<summary>One unit of work's write to a memory image state store's tables, prepared: its rows, serialized when the unit of work prepared.<br/>
/// At the very end of its transaction's prepare, it takes the commit's number from the commit counter, then writes the rows with it.</summary>
///<remarks>It takes part in the transaction at phase 0, enlisted while the unit of work prepares, also at phase 0: a participant<br/>
/// enlisted during phase 0 prepares after every one enlisted before, so after every other participant that writes the database<br/>
/// at its prepare, such as the document db's. So a transaction takes the counter after every other lock it takes, and, holding the<br/>
/// counter, writes only the rows of taggregates whose locks it holds: it waits for nothing another transaction could hold while<br/>
/// it waits for the counter. The database commits after it, as the transaction's decision.</remarks>
sealed class PreparedTableWrite : VolatileTransactionParticipant, IPreparedWrite
{
   readonly IMemoryImageStateStoreSqlLayer _sqlLayer;
   readonly IReadOnlyList<TaggregateRowToSave> _saved;
   readonly IReadOnlyList<TaggregateId> _deleted;

   PreparedTableWrite(IMemoryImageStateStoreSqlLayer sqlLayer, IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted) : base(EnlistmentOptions.EnlistDuringPrepareRequired)
   {
      _sqlLayer = sqlLayer;
      _saved = saved;
      _deleted = deleted;
   }

   ///<summary>A write of <paramref name="saved"/> and <paramref name="deleted"/>, taking part in the ambient transaction, which is<br/>
   /// preparing, unless it writes nothing.</summary>
   internal static PreparedTableWrite TakingPartInTheAmbientTransaction(IMemoryImageStateStoreSqlLayer sqlLayer, IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted)
   {
      var write = new PreparedTableWrite(sqlLayer, saved, deleted);
      if(!write.WritesNothing) write.EnsureEnlistedInAnyAmbientTransaction();
      return write;
   }

   bool WritesNothing => _saved.Count == 0 && _deleted.Count == 0;

   public CommitNumber? CommittedAs { get; private set; }

   ///<summary>The very end of the transaction's prepare: takes the commit's number, then writes the rows with it.</summary>
   protected override void OnPrepare()
   {
      var committedAs = _sqlLayer.TakeTheNextCommitNumber();
      _sqlLayer.Write(committedAs, _saved, _deleted);
      CommittedAs = committedAs;
   }

   protected override void OnCommit() {}
   protected override void OnRollback() {}

   ///<summary>The unit of work commits: the database has committed the rows, as the transaction's decision, before it.</summary>
   public void Commit() => State.Assert(WritesNothing || CommittedAs != null, () => "A write to a memory image state store's tables committed without taking a commit number.");

   ///<summary>The unit of work rolls back, and the database with it.</summary>
   public void Discard() {}
}
