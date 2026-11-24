using System.Transactions;
using Compze.Contracts;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.MySql._private;
using MySqlConnector;

namespace Compze.Sql.MySql._internal;

interface ICompzeMySqlConnection : IPoolableConnection, ICompzeDbConnection<MySqlCommand>
{
   public static ICompzeMySqlConnection Create(string connString) => new CompzeMySqlConnection(connString);

   ///<summary>A connection to MySQL. Opened in an ambient transaction, it begins a transaction of its own, which takes part in the<br/>
   /// ambient one as its decision (<see cref="MySqlTransactionAsTheDecision"/>), and every command it creates runs in it.</summary>
   public sealed class CompzeMySqlConnection : ICompzeMySqlConnection
   {
      MySqlConnection Connection { get; }
      MySqlTransactionAsTheDecision? _transaction;

      internal CompzeMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

      public void Open()
      {
         var ambientTransaction = Transaction.Current;
         Connection.Open();
         if(ambientTransaction != null) _transaction = new MySqlTransactionAsTheDecision(MySqlTransactionAsTheDecision.BeginDatabaseTransaction(Connection, ambientTransaction), ambientTransaction);
      }

      public async Task OpenAsync()
      {
         var ambientTransaction = Transaction.Current;
         await Connection.OpenAsync().caf();
         if(ambientTransaction != null) _transaction = new MySqlTransactionAsTheDecision(await MySqlTransactionAsTheDecision.BeginDatabaseTransactionAsync(Connection, ambientTransaction).caf(), ambientTransaction);
      }

      public MySqlCommand CreateCommand()
      {
         var command = Connection.CreateCommand();
         _transaction?.Includes(command);
         return command;
      }

      public void Dispose()
      {
         AssertTheTransactionHasEnded();
         _transaction?.Dispose();
         Connection.Dispose();
      }

      public async ValueTask DisposeAsync()
      {
         AssertTheTransactionHasEnded();
         _transaction?.Dispose();
         await Connection.DisposeAsync().caf();
      }

      void AssertTheTransactionHasEnded() =>
         Contract.State.Assert(_transaction is not { HasEnded: false }, () => "A MySQL connection was disposed while its transaction hadn't committed or rolled back.");
   }
}
