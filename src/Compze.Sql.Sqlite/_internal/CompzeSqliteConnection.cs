using System.Collections.Concurrent;
using System.Data.Common;
using System.Transactions;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Microsoft.Data.Sqlite;
using IsolationLevel = System.Data.IsolationLevel;

namespace Compze.Sql.Sqlite._internal;

interface ICompzeSqliteConnection : IPoolableConnection, ICompzeDbConnection<SqliteCommand>
{
   public static ICompzeSqliteConnection Create(string connString) => new CompzeSqliteConnection(connString);

   public sealed class CompzeSqliteConnection : ICompzeSqliteConnection
   {
      // Wait this long for a contended lock before giving up, instead of failing the instant the lock is unavailable.
      // Turns transient writer contention into a short wait that almost always resolves. Has no effect on shared-cache
      // in-memory table locks (SQLite does not invoke the busy handler for those); the connection pool retries those.
      const int BusyTimeoutMilliseconds = 5000;

      // SQLite permits one writer per database. We serialise whole write transactions in-process, per database,
      // behind this gate so two threads never contend for the database's write lock at the engine level. That
      // contention is what produced the residual deadlock (a writer holding the write lock while a second writer,
      // mid-mint, waited for it). One gate per database (keyed by connection string), for the process lifetime —
      // the same lifetime as the connection pools, which are likewise cached per connection string.
      static readonly ConcurrentDictionary<string, SemaphoreSlim> WriteGatesByDatabase = new();

      SqliteConnection Connection { get; }
      SqliteTransaction? _transaction;
      readonly VolatileLambdaTransactionParticipant _transactionParticipant;
      // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
#pragma warning disable CA2213 // Shared, per-database, process-lifetime semaphore owned by the static registry above — never disposed by an individual connection.
      readonly SemaphoreSlim _writeGate;
#pragma warning restore CA2213
      readonly bool _isFileDatabase;

      internal CompzeSqliteConnection(string connectionString)
      {
         Connection = new SqliteConnection(connectionString);
         _isFileDatabase = new SqliteConnectionStringBuilder(connectionString).Mode != SqliteOpenMode.Memory;
         _writeGate = WriteGatesByDatabase.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));

         _transactionParticipant = new VolatileLambdaTransactionParticipant(
            enlistmentOptions: EnlistmentOptions.None,
            // Hold the write gate for the whole transaction: acquired before the transaction can take any lock,
            // released only once it has committed or rolled back. Reads outside a transaction are not gated and
            // still run concurrently.
            onEnlist: () =>
            {
               _writeGate.Wait();
               try
               {
                  _transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
               }
               catch
               {
                  _writeGate.Release();
                  throw;
               }
            },
            onPrepare: () => {}, // Nothing to do in prepare - SQLite doesn't support two-phase commit
            onCommit: () =>
            {
               try
               {
                  _transaction!.Commit();
                  _transaction.Dispose();
                  _transaction = null;
               }
               finally
               {
                  _writeGate.Release();
               }
            },
            onRollback: () =>
            {
               try
               {
                  _transaction!.Rollback();
                  _transaction.Dispose();
                  _transaction = null;
               }
               finally
               {
                  _writeGate.Release();
               }
            });
      }

      // Enlistment is deliberately NOT done here. The connection pool opens the connection while holding its
      // per-transaction monitor, and enlistment acquires the write gate (above) — taking the gate under that
      // monitor would invert against threads that hold the gate and then need the monitor. CreateCommand enlists
      // instead (idempotently), and it runs after the pool has released the monitor.
      public void Open()
      {
         Contract.State.NotDisposed(_disposed, this);
         Connection.Open();
         ConfigureConnection();
      }

      public async Task OpenAsync()
      {
         Contract.State.NotDisposed(_disposed, this);
         await Connection.OpenAsync().caf();
         await ConfigureConnectionAsync().caf();
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
