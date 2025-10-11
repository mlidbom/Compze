using System.Data.Common;
using System.Transactions;
using Compze.Sql.Common.Abstractions;
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
      VolatileLambdaTransactionParticipant? _transactionParticipant;

      internal CompzeSqliteConnection(string connectionString) => Connection = new SqliteConnection(connectionString);

      public void Open()
      {
         Connection.Open();
         EnlistInAmbientTransaction();
      }

      public async Task OpenAsync()
      {
         await Connection.OpenAsync().caf();
         EnlistInAmbientTransaction();
      }

      void EnlistInAmbientTransaction()
      {
         var ambientTransaction = Transaction.Current;
         if(ambientTransaction != null && _transaction == null)
         {
            // Start an explicit SQLite transaction
            _transaction = Connection.BeginTransaction();

            // Create a participant that will commit or rollback the SQLite transaction
            _transactionParticipant = new VolatileLambdaTransactionParticipant(
               enlistmentOptions: EnlistmentOptions.None,
               onPrepare: () =>
               {
                  // Nothing to do in prepare - SQLite doesn't support two-phase commit
               },
               onCommit: () =>
               {
                  _transaction?.Commit();
                  _transaction?.Dispose();
                  _transaction = null;
               },
               onRollback: () =>
               {
                  _transaction?.Rollback();
                  _transaction?.Dispose();
                  _transaction = null;
               });

            // Enlist in the ambient transaction
            _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
         }
      }

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();
      
      public SqliteCommand CreateCommand()
      {
         var command = Connection.CreateCommand();
         if(_transaction != null)
         {
            command.Transaction = _transaction;
         }
         return command;
      }

      public void Dispose()
      {
         _transaction?.Dispose();
         Connection.Dispose();
      }

      public ValueTask DisposeAsync()
      {
         _transaction?.Dispose();
         return Connection.DisposeAsync();
      }
   }
}
