using Compze.Abstractions;
using Compze.Threading;

namespace Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;

///<summary>A memory image state store's tables in one database's SQL: implemented for SQL Server, PostgreSQL, MySQL and SQLite, each in a<br/>
/// package of its own, on the connection pool of the domain database. See the tables' design, the-memory-image-state-store-in-tables.md<br/>
/// in Compze's dev_docs.</summary>
///<remarks>What runs outside any transaction runs on a connection of its own when the caller suppresses the ambient transaction;<br/>
/// what runs inside one runs on the transaction's connection. The layer itself suppresses nothing.</remarks>
interface IMemoryImageStateStoreSqlLayer
{
   ///<summary>Every row of the classes named <paramref name="classNames"/>.</summary>
   IReadOnlyList<TaggregateRow> ReadAll(IReadOnlyList<string> classNames);

   ///<summary>Inside the ambient transaction: writes the row of each of <paramref name="saved"/>, adding it for a new taggregate, and deletes<br/>
   /// the row of each of <paramref name="deleted"/>.</summary>
   void Write(IReadOnlyList<TaggregateRowToSave> saved, IReadOnlyList<TaggregateId> deleted);

   ///<summary>Takes the domain database's process lock named <paramref name="lockName"/> for <paramref name="purpose"/>, waiting at most<br/>
   /// <paramref name="waitTimeout"/> for another holder to release it, and returns the hold, whose disposal releases it: or null if another<br/>
   /// holder still holds it. <paramref name="onLostWhileHeld"/> reports the lock lost without being released, as when the database session<br/>
   /// holding it dies. The database package's process lock, which an exactly-once endpoint takes too, under another name.</summary>
   IDisposable? TryTakeProcessLock(string lockName, string purpose, WaitTimeout waitTimeout, Action<Exception> onLostWhileHeld);

   ///<summary>Who holds the process lock named <paramref name="lockName"/>, as it recorded itself: what for, which process, on which machine,<br/>
   /// and since when; null when it took the lock this instant and hasn't recorded itself yet. Read when a take is refused.</summary>
   string? ProcessLockHolder(string lockName);

   ///<summary>The names of the tables and their columns, the same in every database.</summary>
   static class TableSchemaStrings
   {
      internal const string TaggregateTable = "MemoryImageStateStoreTaggregate";
      internal const string Id = nameof(Id);
      internal const string ClassName = nameof(ClassName);
      internal const string Snapshot = nameof(Snapshot);

      ///<summary>The longest class name, as the tables' column holds it.</summary>
      internal const int ClassNameLength = 400;
   }
}
