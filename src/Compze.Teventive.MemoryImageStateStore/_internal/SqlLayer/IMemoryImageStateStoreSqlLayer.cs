using Compze.Abstractions;

namespace Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;

///<summary>A memory image state store's tables in one database's SQL: implemented for SQL Server, PostgreSQL, MySQL and SQLite, each in a<br/>
/// package of its own, on the connection pool of the domain database. See the tables' design, the-memory-image-state-store-in-tables.md<br/>
/// in Compze's dev_docs.</summary>
///<remarks>What runs outside any transaction runs on a connection of its own when the caller suppresses the ambient transaction;<br/>
/// what runs inside one runs on the transaction's connection. The layer itself suppresses nothing.</remarks>
interface IMemoryImageStateStoreSqlLayer
{
   ///<summary>What tells this database from every other: the connection string Compze's connections to it open with. Memory images on<br/>
   /// one machine that name the database the same share its commit signal.</summary>
   string DatabaseIdentity { get; }

   ///<summary>The last commit number, then every row of the classes named <paramref name="classNames"/>, deleted ones included.</summary>
   ///<remarks>The counter is read first, in a statement of its own: every commit up to its value was committed before the rows are<br/>
   /// read, so the rows hold every one of them, whatever isolation the database reads at.</remarks>
   TaggregateRows ReadAll(IReadOnlyList<string> classNames);

   ///<summary>The last commit number, then the rows of the classes named <paramref name="classNames"/> that commits after<br/>
   /// <paramref name="lastSeen"/> wrote: as <see cref="ReadAll"/>, for the commits after a number.</summary>
   TaggregateRows ReadCommittedAfter(CommitNumber lastSeen, IReadOnlyList<string> classNames);

   ///<summary>Inside the ambient transaction: takes the taggregate <paramref name="id"/>'s lock, held until the transaction completes,<br/>
   /// then reads its row unless the commit <paramref name="heldAs"/> wrote it: null when it has no row, or that commit wrote it.</summary>
   TaggregateRow? LockAndReadUnlessHeldAs(TaggregateId id, CommitNumber? heldAs);

   ///<summary>Inside the ambient transaction, at its very end: takes the next commit number from the commit counter, whose lock the<br/>
   /// transaction holds until it completes, so that commits commit in the order of their numbers.</summary>
   CommitNumber TakeTheNextCommitNumber();

   ///<summary>Inside the ambient transaction, once it has taken <paramref name="committedAs"/>: writes the row of each of<br/>
   /// <paramref name="saved"/>, adding it for a new taggregate, and marks the row of each of <paramref name="deleted"/> deleted.<br/>
   /// Every row written holds <paramref name="committedAs"/>.</summary>
   void Write(CommitNumber committedAs, IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted);

   ///<summary>The names of the tables and their columns, the same in every database.</summary>
   ///<remarks>review-naming: MemoryImageStateStoreTaggregate, MemoryImageStateStoreCommitCounter, ClassName, Snapshot and CommitNumber, and MySQL's<br/>
   /// MemoryImageStateStoreTaggregateLock (state-store-in-tables, 2026-11-24).</remarks>
   static class TableSchemaStrings
   {
      internal const string TaggregateTable = "MemoryImageStateStoreTaggregate";
      internal const string Id = nameof(Id);
      internal const string ClassName = nameof(ClassName);
      internal const string Snapshot = nameof(Snapshot);
      internal const string CommitNumber = nameof(CommitNumber);

      internal const string CommitCounterTable = "MemoryImageStateStoreCommitCounter";
      internal const string LastCommitNumber = nameof(LastCommitNumber);

      ///<summary>The id of the commit counter's one row.</summary>
      internal const int TheCounterRow = 1;

      ///<summary>The longest class name, as the tables' column holds it.</summary>
      internal const int ClassNameLength = 400;
   }
}
