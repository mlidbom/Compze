using System.Transactions;
using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using MySqlConnector;

namespace Compze.Sql.MySql._internal;

interface ICompzeMySqlConnection : IPoolableConnection, ICompzeDbConnection<MySqlCommand>
{
   public static ICompzeMySqlConnection Create(string connString) => new CompzeMySqlConnection(connString);

   ///<summary>A connection to MySQL. Opened inside an ambient transaction, it begins a transaction of its own, which takes part in the<br/>
   /// ambient one as its decision (<see cref="DatabaseTransactionAsTheDecision"/>), and every command it creates runs in it.</summary>
   ///<remarks>Compze takes part in the ambient transaction itself, rather than letting MySqlConnector enlist the connection<br/>
   /// (<see cref="CompzeMySqlConnectionString"/> turns that off): MySqlConnector's enlistment commits only when told the outcome,<br/>
   /// after the participants told before it.</remarks>
   public sealed class CompzeMySqlConnection : ICompzeMySqlConnection
   {
      MySqlConnection Connection { get; }
      MySqlTransaction? _transaction;

      internal CompzeMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

      public void Open()
      {
         var ambientTransaction = Transaction.Current;
         Connection.Open();
         if(ambientTransaction != null) TakePartIn(ambientTransaction, Connection.BeginTransaction(DataIsolationLevelOf(ambientTransaction)));
      }

      public async Task OpenAsync()
      {
         var ambientTransaction = Transaction.Current;
         await Connection.OpenAsync().caf();
         if(ambientTransaction != null) TakePartIn(ambientTransaction, await Connection.BeginTransactionAsync(DataIsolationLevelOf(ambientTransaction)).caf());
      }

      public MySqlCommand CreateCommand()
      {
         var command = Connection.CreateCommand();
         command.Transaction = _transaction;
         return command;
      }

      void TakePartIn(Transaction ambientTransaction, MySqlTransaction transaction)
      {
         _transaction = transaction;
         try
         {
            DatabaseTransactionAsTheDecision.TakePartIn(ambientTransaction, "a MySQL connection", commit: Commit, rollBack: RollBack, isAFailureThatLeftItUncommitted: IsAnErrorTheServerAnsweredWith);
         }
         catch
         {
            EndTheTransaction();
            throw;
         }
      }

      ///<summary>Whether <paramref name="exception"/> is an error the server answered with, so that it certainly didn't commit: a broken<br/>
      /// connection, whose exception carries no SQL state, leaves unknown whether the commit reached the server.</summary>
      static bool IsAnErrorTheServerAnsweredWith(Exception exception) => exception is MySqlException { SqlState: not null };

      void Commit()
      {
         try
         {
            _transaction!.Commit();
         }
         finally
         {
            EndTheTransaction();
         }
      }

      void RollBack()
      {
         try
         {
            _transaction!.Rollback();
         }
         finally
         {
            EndTheTransaction();
         }
      }

      void EndTheTransaction()
      {
         _transaction?.Dispose();
         _transaction = null;
      }

      static System.Data.IsolationLevel DataIsolationLevelOf(Transaction transaction) =>
         transaction.IsolationLevel switch
         {
            IsolationLevel.ReadCommitted   => System.Data.IsolationLevel.ReadCommitted,
            IsolationLevel.ReadUncommitted => System.Data.IsolationLevel.ReadUncommitted,
            IsolationLevel.RepeatableRead  => System.Data.IsolationLevel.RepeatableRead,
            IsolationLevel.Serializable    => System.Data.IsolationLevel.Serializable,
            IsolationLevel.Snapshot        => System.Data.IsolationLevel.Snapshot,
            _                              => throw new NotSupportedException($"A MySQL connection can't take part in a transaction at isolation level {transaction.IsolationLevel}.")
         };

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}
