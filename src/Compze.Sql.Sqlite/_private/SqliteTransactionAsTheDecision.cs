using System.Transactions;
using Compze.Sql.Common._internal;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using IsolationLevel = System.Data.IsolationLevel;

namespace Compze.Sql.Sqlite._private;

///<summary>The transaction a SQLite connection begins in its database to take part in an ambient transaction, as that transaction's<br/>
/// decision (<see cref="DatabaseTransactionAsTheDecision"/>), and the database's <see cref="SqliteWriteGate"/>, which it holds from<br/>
/// before it begins until it ends.</summary>
///<remarks>It begins with BEGIN IMMEDIATE, which Microsoft.Data.Sqlite issues for read committed, so it holds the database's write lock<br/>
/// from its start, across processes too. A commit that fails leaves it uncommitted: SQLite runs in the process, so no connection can<br/>
/// break between the commit and its answer.</remarks>
sealed class SqliteTransactionAsTheDecision : DatabaseTransactionAsTheDecision
{
   readonly SqliteWriteGate _writeGate;
   readonly SqliteConnection _connection;

   SqliteTransactionAsTheDecision(SqliteConnection connection, SqliteTransaction databaseTransaction, Transaction ambientTransaction, SqliteWriteGate writeGate)
      : base(databaseTransaction, ambientTransaction, "a SQLite connection")
   {
      _connection = connection;
      _writeGate = writeGate;
      TakePartInTheAmbientTransaction();
   }

   ///<summary>Takes <paramref name="writeGate"/>, begins a transaction on <paramref name="connection"/>, and has it take part in<br/>
   /// <paramref name="ambientTransaction"/> as its decision.</summary>
   internal static SqliteTransactionAsTheDecision Begin(SqliteConnection connection, SqliteWriteGate writeGate, Transaction ambientTransaction)
   {
      writeGate.Take();
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
