using System.Data.Common;
using System.Transactions;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Microsoft.Data.Sqlite;
using IsolationLevel = System.Data.IsolationLevel;

namespace Compze.Internals.Sql.Sqlite;

public interface ICompzeSqliteConnection : IPoolableConnection, ICompzeDbConnection<SqliteCommand>
{
   public static ICompzeSqliteConnection Create(string connString) => new CompzeSqliteConnection(connString);

   public sealed class CompzeSqliteConnection : ICompzeSqliteConnection
   {
      // Wait this long for a contended lock before giving up, instead of failing the instant the lock is unavailable.
      // Turns transient writer contention into a short wait that almost always resolves. Has no effect on shared-cache
      // in-memory table locks (SQLite does not invoke the busy handler for those); the connection pool retries those.
      const int BusyTimeoutMilliseconds = 5000;

      SqliteConnection Connection { get; }
      SqliteTransaction? _transaction;
      readonly VolatileLambdaTransactionParticipant _transactionParticipant;
      readonly bool _isFileDatabase;

      internal CompzeSqliteConnection(string connectionString)
      {
         Connection = new SqliteConnection(connectionString);
         _isFileDatabase = new SqliteConnectionStringBuilder(connectionString).Mode != SqliteOpenMode.Memory;

         _transactionParticipant = new VolatileLambdaTransactionParticipant(
            enlistmentOptions: EnlistmentOptions.None,
            onEnlist: () => _transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted),
            onPrepare: () => {}, // Nothing to do in prepare - SQLite doesn't support two-phase commit
            onCommit: () =>
            {
               _transaction!.Commit();
               _transaction.Dispose();
               _transaction = null;
            },
            onRollback: () =>
            {
               _transaction!.Rollback();
               _transaction.Dispose();
               _transaction = null;
            });
      }

      public void Open()
      {
         Contract.State.NotDisposed(_disposed, this);
         Connection.Open();
         ConfigureConnection();
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
      }

      public async Task OpenAsync()
      {
         Contract.State.NotDisposed(_disposed, this);
         await Connection.OpenAsync().caf();
         await ConfigureConnectionAsync().caf();
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
      }

      // Run before enlisting in any transaction: journal_mode cannot be changed inside a transaction.
      void ConfigureConnection()
      {
         using var command = Connection.CreateCommand();
         command.CommandText = ConfigurationPragmas;
         command.ExecuteNonQuery();
      }

      async Task ConfigureConnectionAsync()
      {
         var command = Connection.CreateCommand();
         await using var _ = command.caf();
         command.CommandText = ConfigurationPragmas;
         await command.ExecuteNonQueryAsync().caf();
      }

      // WAL lets readers run concurrently with the single writer, so a read-then-write transaction never deadlocks a
      // committing writer the way the default rollback journal does. synchronous=NORMAL is the safe companion to WAL:
      // it stops fsync-ing on every commit (only checkpoints fsync), which under concurrent load is what otherwise
      // makes a writer hold the single write lock across a slow disk flush and starve everyone else waiting for it.
      // Both are unavailable/irrelevant for in-memory databases, which rely on the connection pool's transient-lock retry.
      string ConfigurationPragmas =>
         _isFileDatabase
            ? $"PRAGMA busy_timeout={BusyTimeoutMilliseconds}; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;"
            : $"PRAGMA busy_timeout={BusyTimeoutMilliseconds};";

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();

      public SqliteCommand CreateCommand()
      {
         Contract.State.NotDisposed(_disposed, this);
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();

         var command = Connection.CreateCommand();
         if(_transaction != null)
         {
            command.Transaction = _transaction;
         }

         return command;
      }

      bool _disposed = false;
      public void Dispose()
      {
         if(!_disposed)
         {
            _disposed = true;
            Contract.State.Assert(_transaction == null, () => "Transaction should have been completed (committed or rolled back) before disposing the connection");
            Connection.Dispose();
         }
      }

      public async ValueTask DisposeAsync()
      {
         if(!_disposed)
         {
            _disposed = true;
            Contract.State.Assert(_transaction == null, () => "Transaction should have been completed (committed or rolled back) before disposing the connection");
            await Connection.DisposeAsync().caf();
         }
      }
   }
}
