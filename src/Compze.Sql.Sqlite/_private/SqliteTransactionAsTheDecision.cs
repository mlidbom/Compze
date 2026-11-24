using System.Transactions;
using Compze.Sql.Common._internal;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using IsolationLevel = System.Data.IsolationLevel;

namespace Compze.Sql.Sqlite._private;

///<summary>The transaction a SQLite connection begins in its database to take part in an ambient transaction, as that transaction's<br/>
/// decision (<see cref="DatabaseTransactionAsTheDecision"/>), and the database's write gate, which it holds from before it begins until<br/>
/// it ends.</summary>
///<remarks>It begins with BEGIN IMMEDIATE, which Microsoft.Data.Sqlite issues for read committed, so it holds the database's write lock<br/>
/// from its start, across processes too. A commit that fails leaves it uncommitted: SQLite runs in the process, so no connection can<br/>
/// break between the commit and its answer.</remarks>
sealed class SqliteTransactionAsTheDecision : DatabaseTransactionAsTheDecision
{
#pragma warning disable CA2213 //The write gate is the database's, shared by every connection to it for the process's lifetime: a transaction only takes and releases it.
   readonly SemaphoreSlim _writeGate;
#pragma warning restore CA2213
   readonly SqliteConnection _connection;

   SqliteTransactionAsTheDecision(SqliteConnection connection, SqliteTransaction databaseTransaction, Transaction ambientTransaction, SemaphoreSlim writeGate)
      : base(databaseTransaction, ambientTransaction, "a SQLite connection")
   {
      _connection = connection;
      _writeGate = writeGate;
      TakePartInTheAmbientTransaction();
   }

   ///<summary>Takes <paramref name="writeGate"/>, begins a transaction on <paramref name="connection"/>, and has it take part in<br/>
   /// <paramref name="ambientTransaction"/> as its decision.</summary>
   internal static SqliteTransactionAsTheDecision Begin(SqliteConnection connection, SemaphoreSlim writeGate, Transaction ambientTransaction)
   {
      //urgent: this waits for the write gate with no timeout, and nothing records who holds it. A transaction that hangs, in one
      //   endpoint of a process, blocks every other transaction on the same database in that process forever, and nothing says which
      //   one holds it: across processes SQLite's own busy_timeout gives up after 5 s, inside one process nothing does. Give it a
      //   timeout whose exception names the holder: its transaction, its thread, where and when it took the gate. The gate is held
      //   for a whole transaction and released on whatever thread completes it, so it needs the Compze.Threading lock owned by a
      //   transaction that TaggregateUpdateLocks' todo asks for (Magnus, 2026-11-24).
      writeGate.Wait();
      SqliteTransaction databaseTransaction;
      try
      {
         databaseTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
      }
      catch
      {
         writeGate.Release();
         throw;
      }

      return new SqliteTransactionAsTheDecision(connection, databaseTransaction, ambientTransaction, writeGate);
   }

   protected override bool CertainlyDidNotCommitAfter(Exception commitFailure) => true;

   ///<remarks>When SQLite turns a commit into a rollback itself, as it does when a commit hook refuses the commit, Microsoft.Data.Sqlite<br/>
   /// may not notice: disposing its transaction then runs a ROLLBACK, which fails, finding no transaction, and completes the transaction<br/>
   /// as it fails. SQLite has no transaction when the connection is back in autocommit mode.</remarks>
   protected override bool TheDatabaseHadRolledBackItselfWhen(Exception rollbackFailure) =>
      rollbackFailure is SqliteException && raw.sqlite3_get_autocommit(_connection.Handle) != 0;

   protected override void OnEnded() => _writeGate.Release();
}
