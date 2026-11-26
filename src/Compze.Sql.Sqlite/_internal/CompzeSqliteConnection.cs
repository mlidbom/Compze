using System.Transactions;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.Sqlite._private;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite._internal;

interface ICompzeSqliteConnection : IPoolableConnection, ICompzeDbConnection<SqliteCommand>
{
   public static ICompzeSqliteConnection Create(string connString) => new CompzeSqliteConnection(connString);

   public sealed class CompzeSqliteConnection : ICompzeSqliteConnection
   {
      ///<summary>How long SQLite waits for a contended lock before giving up, instead of failing the instant the lock is unavailable.<br/>
      /// Turns transient writer contention into a short wait that almost always resolves.</summary>
      ///<remarks>Has no effect on shared-cache in-memory table locks: SQLite does not invoke the busy handler for those. The connection<br/>
      /// pool retries those.</remarks>
      const int BusyTimeoutMilliseconds = 5000;

      SqliteConnection Connection { get; }
      SqliteTransactionAsTheDecision? _transaction;
      readonly SqliteWriteGate _writeGate;
      readonly bool _isFileDatabase;

      internal CompzeSqliteConnection(string connectionString)
      {
         Connection = new SqliteConnection(connectionString);
         _isFileDatabase = new SqliteConnectionStringBuilder(connectionString).Mode != SqliteOpenMode.Memory;
         _writeGate = SqliteWriteGate.OfTheDatabase(connectionString);
      }

      ///<summary>Begins this connection's transaction, taking part in the ambient transaction as its decision, if there is an ambient<br/>
      /// transaction and the connection hasn't begun it yet: see <see cref="SqliteTransactionAsTheDecision"/>. Reads outside a transaction<br/>
      /// take no write gate, and run concurrently.</summary>
      void TakePartInAnyAmbientTransaction()
      {
         var ambientTransaction = Transaction.Current;
         if(ambientTransaction == null) return;

         _transaction ??= SqliteTransactionAsTheDecision.Begin(Connection, _writeGate, ambientTransaction);
         AssertTakesPartIn(ambientTransaction);
      }

      ///<summary>As <see cref="TakePartInAnyAmbientTransaction"/>, holding no thread while another transaction holds the write gate.</summary>
      async Task TakePartInAnyAmbientTransactionAsync()
      {
         var ambientTransaction = Transaction.Current;
         if(ambientTransaction == null) return;

         if(_transaction == null)
         {
            await _writeGate.TakeAsync().caf();
            _transaction = SqliteTransactionAsTheDecision.BeginWithTheGateTaken(Connection, _writeGate, ambientTransaction);
         }
         AssertTakesPartIn(ambientTransaction);
      }

      void AssertTakesPartIn(Transaction ambientTransaction) =>
         Contract.State.Assert(_transaction!.AmbientTransaction.Equals(ambientTransaction),
                               () => $"A SQLite connection taking part in transaction {_transaction.AmbientTransaction.TransactionInformation.LocalIdentifier} was used in another one, {ambientTransaction.TransactionInformation.LocalIdentifier}.");

      ///<remarks>Taking part in the ambient transaction is deliberately not done here. The connection pool opens the connection while<br/>
      /// holding its per-transaction monitor, and taking part takes the <see cref="SqliteWriteGate"/>: taking the gate under that monitor<br/>
      /// would invert against threads that hold the gate and then need the monitor. <see cref="CreateCommand"/> takes part instead,<br/>
      /// idempotently, and it runs after the pool has released the monitor.</remarks>
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

      ///<remarks>Runs before the connection takes part in any transaction: journal_mode cannot be changed inside a transaction.</remarks>
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

      ///<remarks>WAL lets readers run concurrently with the single writer, so a read-then-write transaction never deadlocks a committing<br/>
      /// writer the way the default rollback journal does. synchronous=NORMAL is the safe companion to WAL: it stops fsync-ing on every<br/>
      /// commit (only checkpoints fsync), which under concurrent load is what otherwise makes a writer hold the single write lock across<br/>
      /// a slow disk flush and starve everyone else waiting for it. Both are unavailable/irrelevant for in-memory databases, which rely on<br/>
      /// the connection pool's transient-lock retry.</remarks>
      string ConfigurationPragmas =>
         _isFileDatabase
            ? $"PRAGMA busy_timeout={BusyTimeoutMilliseconds}; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;"
            : $"PRAGMA busy_timeout={BusyTimeoutMilliseconds};";


      public SqliteCommand CreateCommand()
      {
         Contract.State.NotDisposed(_disposed, this);
         TakePartInAnyAmbientTransaction();
         return CreateCommandInAnyTransaction();
      }

      public async Task<SqliteCommand> CreateCommandAsync()
      {
         Contract.State.NotDisposed(_disposed, this);
         await TakePartInAnyAmbientTransactionAsync().caf();
         return CreateCommandInAnyTransaction();
      }

      SqliteCommand CreateCommandInAnyTransaction()
      {
         var command = Connection.CreateCommand();
         _transaction?.Includes(command);
         return command;
      }

      bool _disposed = false;
      public void Dispose()
      {
         if(!_disposed)
         {
            _disposed = true;
            AssertTheTransactionHasEnded();
            _transaction?.Dispose();
            Connection.Dispose();
         }
      }

      public async ValueTask DisposeAsync()
      {
         if(!_disposed)
         {
            _disposed = true;
            AssertTheTransactionHasEnded();
            _transaction?.Dispose();
            await Connection.DisposeAsync().caf();
         }
      }

      void AssertTheTransactionHasEnded() =>
         Contract.State.Assert(_transaction is not { HasEnded: false }, () => "A SQLite connection was disposed while its transaction hadn't committed or rolled back.");
   }
}
