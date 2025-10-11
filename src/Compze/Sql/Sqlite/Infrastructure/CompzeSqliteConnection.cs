using System.Data.Common;
using System.Transactions;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Infrastructure;

internal interface ICompzeSqliteConnection : IPoolableConnection, ICompzeDbConnection<SqliteCommand>
{
   internal static ICompzeSqliteConnection Create(string connString) => new CompzeSqliteConnection(connString);

   sealed class CompzeSqliteConnection : ICompzeSqliteConnection
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
         Connection.Open();
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
      }

      public async Task OpenAsync()
      {
         await Connection.OpenAsync().caf();
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
      }

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();

      public SqliteCommand CreateCommand()
      {
         _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();

         var command = Connection.CreateCommand();
         if(_transaction != null)
         {
            command.Transaction = _transaction;
         }

         return command;
      }

      public void Dispose()
      {
         Assert.State.Is(_transaction == null, () => "Transaction should have been completed (committed or rolled back) before disposing the connection");
         Connection.Dispose();
      }

      public ValueTask DisposeAsync()
      {
         Assert.State.Is(_transaction == null, () => "Transaction should have been completed (committed or rolled back) before disposing the connection");
         return Connection.DisposeAsync();
      }
   }
}
