using System.Collections.Concurrent;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Sql.Sqlite._private;

///<summary>The gate a SQLite database's transactions pass one at a time in this process: each takes it before it begins, and releases<br/>
/// it once it has committed or rolled back (<see cref="SqliteTransactionAsTheDecision"/>). Reads outside a transaction don't take it,<br/>
/// and run concurrently.</summary>
///<remarks>SQLite permits one writer per database. Serializing whole transactions in the process, per database, keeps two threads<br/>
/// from contending for the database's write lock in SQLite itself. That contention is what produced the residual deadlock: a writer<br/>
/// holding the write lock while a second writer, mid-mint, waited for it.</remarks>
///<remarks>One semaphore per database, keyed by connection string, held for the process's lifetime by <see cref="SemaphoresByDatabase"/>:<br/>
/// the same lifetime as the connection pools, which are likewise cached per connection string. Each gate of a database takes and<br/>
/// releases that database's semaphore.</remarks>
sealed class SqliteWriteGate
{
   static readonly ConcurrentDictionary<string, SemaphoreSlim> SemaphoresByDatabase = new();

   readonly SemaphoreSlim _semaphore;

   SqliteWriteGate(SemaphoreSlim semaphore) => _semaphore = semaphore;

   ///<summary>The write gate of the database <paramref name="connectionString"/> connects to.</summary>
   internal static SqliteWriteGate OfTheDatabase(string connectionString) =>
      new(SemaphoresByDatabase.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1)));

   ///<summary>Waits until no other transaction holds the gate, and takes it.</summary>
   internal void Take()
   {
      //urgent: this waits for the gate with no timeout, and nothing records who holds it. A transaction that hangs, in one endpoint
      //   of a process, blocks every other transaction on the same database in that process forever, and nothing says which one holds
      //   it: across processes SQLite's own busy_timeout gives up after 5 s, inside one process nothing does. Give it a timeout whose
      //   exception names the holder: its transaction, its thread, where and when it took the gate. The gate is held for a whole
      //   transaction and released on whatever thread completes it, so it needs the Compze.Threading lock owned by a transaction that
      //   TaggregateUpdateLocks' todo asks for (Magnus, 2026-11-24).
      _semaphore.Wait();
   }

   ///<summary>As <see cref="Take"/>, holding no thread while another transaction holds the gate.</summary>
   internal async Task TakeAsync()
   {
      //urgent: as Take's: no timeout, and nothing records who holds the gate.
      await _semaphore.WaitAsync().caf();
   }

   internal void Release() => _semaphore.Release();
}
