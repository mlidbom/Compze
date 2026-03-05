using System.Data.Common;
using System.Transactions;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Microsoft.Data.Sqlite;

namespace Compze.Internals.Sql.Sqlite;

public interface ICompzeSqliteConnection : IPoolableConnection, ICompzeDbConnection<SqliteCommand>
{
   public static ICompzeSqliteConnection Create(string connString) => new CompzeSqliteConnection(connString);

   public sealed class CompzeSqliteConnection : ICompzeSqliteConnection
   {
      SqliteConnection Connection { get; }
      SqliteTransaction? _transaction;
      readonly VolatileLambdaTransactionParticipant _transactionParticipant;

      internal CompzeSqliteConnection(string connectionString)
      {
         Connection = new SqliteConnection(connectionString);

         _transactionParticipant = new VolatileLambdaTransactionParticipant(
            enlistmentOptions: EnlistmentOptions.None,
            onEnlist: () => _transaction = Connection.BeginTransaction(System.Data.IsolationLevel.Serializable),
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
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
      }

      public async Task OpenAsync()
      {
         Contract.State.NotDisposed(_disposed, this);
         await Connection.OpenAsync().caf();
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
      }

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
